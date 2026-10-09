import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import '../../models/skill_model.dart';
import '../../providers/auth_provider.dart';
import '../../providers/profile_provider.dart';
import '../../theme/app_theme.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({Key? key}) : super(key: key);

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emergencyContactController = TextEditingController();
  final _bioController = TextEditingController();
  final _maxHoursController = TextEditingController(text: '20');

  final ImagePicker _picker = ImagePicker();
  final Set<String> _selectedSkillIds = {};

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadData());
  }

  @override
  void dispose() {
    _emergencyContactController.dispose();
    _bioController.dispose();
    _maxHoursController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    final profileProvider = Provider.of<ProfileProvider>(context, listen: false);
    await profileProvider.fetchSkills();
    await profileProvider.fetchProfile();

    final profile = profileProvider.profile;
    if (profile != null && mounted) {
      setState(() {
        _emergencyContactController.text = profile.emergencyContact;
        _bioController.text = profile.bio;
        _maxHoursController.text = profile.maxHoursPerWeek.toString();

        _selectedSkillIds.clear();
        for (final skill in profile.skills) {
          _selectedSkillIds.add(skill.id);
        }
      });
    }
  }

  /// Mandatory Device Feature (SE3090 Section 8): Camera & Photo Picker
  void _showImageSourceDialog() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 12),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 40,
                height: 4,
                margin: const EdgeInsets.only(bottom: 12),
                decoration: BoxDecoration(
                  color: Colors.grey.shade300,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              const Padding(
                padding: EdgeInsets.symmetric(horizontal: 20, vertical: 8),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text(
                    'Device Hardware Feature: Avatar Capture',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: AppTheme.textPrimary,
                    ),
                  ),
                ),
              ),
              ListTile(
                leading: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: AppTheme.primaryLight,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.camera_alt, color: AppTheme.primaryColor),
                ),
                title: const Text('Capture with Device Camera', style: TextStyle(fontWeight: FontWeight.w600)),
                subtitle: const Text('Open physical or emulated camera'),
                onTap: () {
                  Navigator.pop(ctx);
                  _pickImage(ImageSource.camera);
                },
              ),
              ListTile(
                leading: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF3E8FF),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.photo_library, color: AppTheme.accentColor),
                ),
                title: const Text('Choose from Photo Gallery', style: TextStyle(fontWeight: FontWeight.w600)),
                subtitle: const Text('Select existing picture from phone storage'),
                onTap: () {
                  Navigator.pop(ctx);
                  _pickImage(ImageSource.gallery);
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _pickImage(ImageSource source) async {
    try {
      final XFile? picked = await _picker.pickImage(
        source: source,
        maxWidth: 800,
        maxHeight: 800,
        imageQuality: 85,
      );

      if (picked != null && mounted) {
        final profileProvider = Provider.of<ProfileProvider>(context, listen: false);
        profileProvider.setAvatarFile(File(picked.path));

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Row(
              children: const [
                Icon(Icons.check_circle, color: Colors.white, size: 18),
                SizedBox(width: 8),
                Text('Profile photo captured from hardware device!'),
              ],
            ),
            backgroundColor: AppTheme.statusApproved,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } catch (e) {
      debugPrint('Error accessing hardware camera/gallery: $e');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Hardware access error: $e'),
            backgroundColor: AppTheme.statusRejected,
          ),
        );
      }
    }
  }

  Future<void> _submitProfile() async {
    if (!_formKey.currentState!.validate()) return;

    final profileProvider = Provider.of<ProfileProvider>(context, listen: false);
    final maxHours = int.tryParse(_maxHoursController.text.trim()) ?? 20;

    final success = await profileProvider.saveProfile(
      emergencyContact: _emergencyContactController.text.trim(),
      bio: _bioController.text.trim(),
      maxHoursPerWeek: maxHours,
      skillIds: _selectedSkillIds.toList(),
    );

    if (!mounted) return;

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Row(
            children: const [
              Icon(Icons.verified_rounded, color: Colors.white),
              SizedBox(width: 10),
              Text('Profile & Skills updated successfully!'),
            ],
          ),
          backgroundColor: AppTheme.statusApproved,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
      );
    } else {
      final error = profileProvider.errorMessage ?? 'Failed to update profile.';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(error),
          backgroundColor: AppTheme.statusRejected,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final profileProvider = Provider.of<ProfileProvider>(context);
    final auth = Provider.of<AuthProvider>(context);
    final user = auth.currentUser;

    return SingleChildScrollView(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Avatar Header Card
            Card(
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
                child: Column(
                  children: [
                    Stack(
                      alignment: Alignment.bottomRight,
                      children: [
                        CircleAvatar(
                          radius: 54,
                          backgroundColor: AppTheme.primaryLight,
                          backgroundImage: profileProvider.avatarFile != null
                              ? FileImage(profileProvider.avatarFile!)
                              : null,
                          child: profileProvider.avatarFile == null
                              ? Text(
                                  (user?.fullName.isNotEmpty == true
                                          ? user!.fullName[0]
                                          : 'V')
                                      .toUpperCase(),
                                  style: const TextStyle(
                                    fontSize: 42,
                                    fontWeight: FontWeight.w700,
                                    color: AppTheme.primaryColor,
                                  ),
                                )
                              : null,
                        ),
                        // Hardware Camera Selector Button
                        Material(
                          color: AppTheme.primaryColor,
                          shape: const CircleBorder(),
                          elevation: 4,
                          child: InkWell(
                            customBorder: const CircleBorder(),
                            onTap: _showImageSourceDialog,
                            child: const Padding(
                              padding: EdgeInsets.all(9),
                              child: Icon(
                                Icons.camera_alt_rounded,
                                color: Colors.white,
                                size: 20,
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 14),
                    Text(
                      user?.fullName ?? 'Volunteer',
                      style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: AppTheme.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      user?.email ?? '',
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppTheme.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                      decoration: BoxDecoration(
                        color: AppTheme.statusApprovedBg,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.star, color: Color(0xFFEAB308), size: 16),
                          const SizedBox(width: 4),
                          Text(
                            'Rating: ${(profileProvider.profile?.ratingScore ?? 5.0).toStringAsFixed(1)} / 5.0',
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: AppTheme.statusApproved,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 20),

            // Profile Details Card
            Card(
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      'Profile Information',
                      style: TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: AppTheme.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 16),

                    // Emergency Contact
                    TextFormField(
                      controller: _emergencyContactController,
                      decoration: const InputDecoration(
                        labelText: 'Emergency Contact (Phone or Name)',
                        prefixIcon: Icon(Icons.emergency_outlined, size: 20),
                        hintText: '+1 (555) 012-3456',
                      ),
                      validator: (val) {
                        if (val == null || val.trim().isEmpty) {
                          return 'Emergency contact is required';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 14),

                    // Max Hours Per Week
                    TextFormField(
                      controller: _maxHoursController,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Max Availability (Hours / Week)',
                        prefixIcon: Icon(Icons.access_time_outlined, size: 20),
                        hintText: '20',
                      ),
                      validator: (val) {
                        final parsed = int.tryParse(val ?? '');
                        if (parsed == null || parsed <= 0 || parsed > 168) {
                          return 'Enter valid hours (1-168)';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 14),

                    // Bio
                    TextFormField(
                      controller: _bioController,
                      maxLines: 3,
                      decoration: const InputDecoration(
                        labelText: 'Volunteer Bio & Experience',
                        prefixIcon: Icon(Icons.info_outline, size: 20),
                        hintText: 'Share your background, logistics or safety experience...',
                        alignLabelWithHint: true,
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 20),

            // Skill Builder Card (Requirement 5)
            Card(
              child: Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: const [
                        Icon(Icons.psychology_outlined, color: AppTheme.primaryColor, size: 22),
                        SizedBox(width: 8),
                        Text(
                          'Interactive Skill Builder',
                          style: TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w700,
                            color: AppTheme.textPrimary,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    const Text(
                      'Select the skills you bring to event teams. These are used for AI matchmaking and crew rostering.',
                      style: TextStyle(fontSize: 13, color: AppTheme.textSecondary),
                    ),
                    const SizedBox(height: 16),

                    Wrap(
                      spacing: 8,
                      runSpacing: 10,
                      children: profileProvider.availableSkills.map((SkillModel skill) {
                        final isSelected = _selectedSkillIds.contains(skill.id) ||
                            _selectedSkillIds.any((id) =>
                                id.toLowerCase() == skill.id.toLowerCase() ||
                                id.toLowerCase() == skill.name.toLowerCase());

                        return FilterChip(
                          label: Text(skill.name),
                          selected: isSelected,
                          selectedColor: AppTheme.primaryLight,
                          checkmarkColor: AppTheme.primaryColor,
                          labelStyle: TextStyle(
                            color: isSelected ? AppTheme.primaryColor : AppTheme.textPrimary,
                            fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                            fontSize: 13,
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                            side: BorderSide(
                              color: isSelected ? AppTheme.primaryColor : AppTheme.borderSubtle,
                              width: isSelected ? 1.5 : 1,
                            ),
                          ),
                          onSelected: (bool selected) {
                            setState(() {
                              if (selected) {
                                _selectedSkillIds.add(skill.id);
                              } else {
                                _selectedSkillIds.remove(skill.id);
                                _selectedSkillIds.removeWhere((id) =>
                                    id.toLowerCase() == skill.id.toLowerCase() ||
                                    id.toLowerCase() == skill.name.toLowerCase());
                              }
                            });
                          },
                        );
                      }).toList(),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            // Submit Button
            ElevatedButton.icon(
              onPressed: profileProvider.isLoading ? null : _submitProfile,
              icon: profileProvider.isLoading
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                    )
                  : const Icon(Icons.save_rounded, size: 20),
              label: const Text('Save Profile & Skills'),
            ),
            const SizedBox(height: 30),
          ],
        ),
      ),
    );
  }
}
