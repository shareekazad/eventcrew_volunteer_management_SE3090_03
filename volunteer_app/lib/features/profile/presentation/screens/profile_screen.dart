import 'package:flutter/material.dart';

import '../../data/models/skill_model.dart';
import '../../data/models/volunteer_profile_model.dart';
import '../../data/repositories/profile_repository.dart';

/// Volunteer profile screen — view + edit, with skill picker.
class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final _repository = ProfileRepository();

  bool _isLoading = true;
  String? _errorMessage;
  VolunteerProfileModel? _profile;
  List<SkillModel> _skillCatalog = [];
  bool _isEditing = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });
    try {
      // Call sequentially (avoids Future.wait generic-type headaches)
      final profile = await _repository.getMyProfile();
      final skills = await _repository.getSkillCatalog();

      if (!mounted) return;
      setState(() {
        _profile = profile;
        _skillCatalog = skills;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Profile'),
        actions: _isEditing
            ? null
            : [
                IconButton(
                  icon: const Icon(Icons.refresh),
                  tooltip: 'Refresh',
                  onPressed: _load,
                ),
              ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline,
                  size: 64, color: Colors.redAccent),
              const SizedBox(height: 16),
              const Text(
                'Could not load profile',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(_errorMessage!, textAlign: TextAlign.center),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: _load,
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (_isEditing) {
      return _ProfileEditForm(
        initial: _profile,
        skillCatalog: _skillCatalog,
        onCancel: () => setState(() => _isEditing = false),
        onSaved: (updated) {
          setState(() {
            _profile = updated;
            _isEditing = false;
          });
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Profile saved'),
              backgroundColor: Colors.green,
            ),
          );
        },
        repository: _repository,
      );
    }

    if (_profile == null) {
      return _buildEmptyState();
    }

    return _buildViewMode();
  }

  Widget _buildEmptyState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.person_outline, size: 64, color: Colors.grey),
            const SizedBox(height: 16),
            const Text(
              'No profile yet',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Set up your profile so organizers can see your skills.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.black54),
            ),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: () => setState(() => _isEditing = true),
              icon: const Icon(Icons.add),
              label: const Text('Create Profile'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildViewMode() {
    final p = _profile!;
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ---- Header card ----
          Card(
            elevation: 1,
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 32,
                    backgroundColor:
                        Theme.of(context).colorScheme.primaryContainer,
                    child: Text(
                      _initials(p.fullName),
                      style: const TextStyle(
                          fontSize: 20, fontWeight: FontWeight.bold),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          p.fullName ?? 'Volunteer',
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(height: 4),
                        if (p.email != null)
                          Text(
                            p.email!,
                            style: const TextStyle(
                                color: Colors.black54, fontSize: 13),
                          ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            const Icon(Icons.star,
                                size: 16, color: Colors.amber),
                            const SizedBox(width: 4),
                            Text(
                              '${p.ratingScore.toStringAsFixed(1)} rating',
                              style: const TextStyle(fontSize: 13),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 20),

          _sectionTitle('Contact'),
          _infoTile(Icons.phone_outlined, 'Emergency Contact',
              p.emergencyContact),
          const SizedBox(height: 20),

          _sectionTitle('Availability'),
          _infoTile(Icons.schedule_outlined, 'Max hours per week',
              '${p.maxHoursPerWeek} hours'),
          const SizedBox(height: 20),

          if (p.bio != null && p.bio!.isNotEmpty) ...[
            _sectionTitle('About'),
            Card(
              elevation: 0,
              color: Colors.grey.shade100,
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: Text(
                  p.bio!,
                  style: const TextStyle(fontSize: 14, height: 1.5),
                ),
              ),
            ),
            const SizedBox(height: 20),
          ],

          _sectionTitle('Skills (${p.skills.length})'),
          if (p.skills.isEmpty)
            const Text('No skills added yet.',
                style: TextStyle(color: Colors.black54))
          else
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: p.skills
                  .map((s) => Chip(
                        label: Text('${s.name} · ${s.proficiencyLevel}'),
                        avatar: const Icon(Icons.workspace_premium, size: 16),
                      ))
                  .toList(),
            ),
          const SizedBox(height: 32),

          SizedBox(
            width: double.infinity,
            height: 52,
            child: FilledButton.icon(
              onPressed: () => setState(() => _isEditing = true),
              icon: const Icon(Icons.edit_outlined),
              label: const Text('Edit Profile'),
            ),
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }

  Widget _sectionTitle(String title) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Text(
          title,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
        ),
      );

  Widget _infoTile(IconData icon, String label, String value) => Card(
        elevation: 0,
        color: Colors.grey.shade100,
        child: ListTile(
          leading: Icon(icon, color: Colors.black54),
          title: Text(label, style: const TextStyle(fontSize: 13)),
          subtitle: Text(
            value,
            style: const TextStyle(
                fontSize: 15, fontWeight: FontWeight.w600, color: Colors.black),
          ),
        ),
      );

  String _initials(String? name) {
    if (name == null || name.trim().isEmpty) return '?';
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.length == 1) return parts[0][0].toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
}

// ============================================================================
// Edit form (with skill picker)
// ============================================================================
class _ProfileEditForm extends StatefulWidget {
  const _ProfileEditForm({
    required this.initial,
    required this.skillCatalog,
    required this.onCancel,
    required this.onSaved,
    required this.repository,
  });

  final VolunteerProfileModel? initial;
  final List<SkillModel> skillCatalog;
  final VoidCallback onCancel;
  final ValueChanged<VolunteerProfileModel> onSaved;
  final ProfileRepository repository;

  @override
  State<_ProfileEditForm> createState() => _ProfileEditFormState();
}

class _ProfileEditFormState extends State<_ProfileEditForm> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _emergencyCtrl;
  late final TextEditingController _bioCtrl;
  late final TextEditingController _hoursCtrl;

  /// IDs of skills currently selected by the volunteer.
  late final Set<String> _selectedSkillIds;

  bool _isSaving = false;
  String? _saveError;

  @override
  void initState() {
    super.initState();
    final p = widget.initial;
    _emergencyCtrl = TextEditingController(text: p?.emergencyContact ?? '');
    _bioCtrl = TextEditingController(text: p?.bio ?? '');
    _hoursCtrl =
        TextEditingController(text: (p?.maxHoursPerWeek ?? 20).toString());
    _selectedSkillIds =
        (p?.skills.map((s) => s.id).toSet()) ?? <String>{};
  }

  @override
  void dispose() {
    _emergencyCtrl.dispose();
    _bioCtrl.dispose();
    _hoursCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _isSaving = true;
      _saveError = null;
    });

    try {
      final updated = await widget.repository.upsertProfile(
        emergencyContact: _emergencyCtrl.text.trim(),
        bio: _bioCtrl.text.trim().isEmpty ? null : _bioCtrl.text.trim(),
        maxHoursPerWeek: int.parse(_hoursCtrl.text.trim()),
        skillIds: _selectedSkillIds.toList(),
      );
      if (!mounted) return;
      widget.onSaved(updated);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _saveError = e.toString();
        _isSaving = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // ---- Emergency contact ----
            const Text('Emergency Contact',
                style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 6),
            TextFormField(
              controller: _emergencyCtrl,
              decoration: const InputDecoration(
                border: OutlineInputBorder(),
                hintText: 'Phone number or "Name: phone"',
              ),
              validator: (v) {
                final value = v?.trim() ?? '';
                if (value.isEmpty) return 'Emergency contact is required';
                if (value.length < 7) return 'Must be at least 7 characters';
                if (value.length > 20) return 'Cannot exceed 20 characters';
                return null;
              },
            ),
            const SizedBox(height: 20),

            // ---- Bio ----
            const Text('Bio', style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 6),
            TextFormField(
              controller: _bioCtrl,
              maxLines: 4,
              maxLength: 1000,
              decoration: const InputDecoration(
                border: OutlineInputBorder(),
                hintText: 'Tell organizers about yourself (optional)',
              ),
            ),
            const SizedBox(height: 12),

            // ---- Max hours ----
            const Text('Max hours per week',
                style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 6),
            TextFormField(
              controller: _hoursCtrl,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(
                border: OutlineInputBorder(),
                hintText: '1 – 168',
              ),
              validator: (v) {
                final n = int.tryParse(v?.trim() ?? '');
                if (n == null) return 'Enter a number';
                if (n < 1 || n > 168) return 'Must be between 1 and 168';
                return null;
              },
            ),
            const SizedBox(height: 24),

            // ---- Skills picker ----
            Row(
              children: [
                const Icon(Icons.workspace_premium, size: 20),
                const SizedBox(width: 6),
                const Text(
                  'Skills',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                ),
                const SizedBox(width: 8),
                if (_selectedSkillIds.isNotEmpty)
                  Text(
                    '(${_selectedSkillIds.length} selected)',
                    style: const TextStyle(color: Colors.black54, fontSize: 13),
                  ),
              ],
            ),
            const SizedBox(height: 8),
            if (widget.skillCatalog.isEmpty)
              const Text(
                'No skills available in the catalog.',
                style: TextStyle(color: Colors.black54),
              )
            else
              _buildSkillChipsByCategory(),
            const SizedBox(height: 24),

            if (_saveError != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.red.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.red.shade200),
                  ),
                  child: Text(
                    _saveError!,
                    style: TextStyle(color: Colors.red.shade700),
                  ),
                ),
              ),

            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: _isSaving ? null : widget.onCancel,
                    child: const Text('Cancel'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FilledButton(
                    onPressed: _isSaving ? null : _save,
                    child: _isSaving
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2.4,
                              color: Colors.white,
                            ),
                          )
                        : const Text('Save Profile'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  /// Groups skills by category and shows one wrap of FilterChips per group.
  Widget _buildSkillChipsByCategory() {
    final byCategory = <String, List<SkillModel>>{};
    for (final s in widget.skillCatalog) {
      byCategory.putIfAbsent(s.category, () => []).add(s);
    }

    final categories = byCategory.keys.toList()..sort();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: categories.map((category) {
        final skills = byCategory[category]!
          ..sort((a, b) => a.name.compareTo(b.name));

        return Padding(
          padding: const EdgeInsets.only(bottom: 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                category,
                style: const TextStyle(
                  fontSize: 12,
                  color: Colors.black54,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 6),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: skills.map((skill) {
                  final selected = _selectedSkillIds.contains(skill.id);
                  return FilterChip(
                    label: Text(skill.name),
                    selected: selected,
                    onSelected: (value) {
                      setState(() {
                        if (value) {
                          _selectedSkillIds.add(skill.id);
                        } else {
                          _selectedSkillIds.remove(skill.id);
                        }
                      });
                    },
                  );
                }).toList(),
              ),
            ],
          ),
        );
      }).toList(),
    );
  }
}