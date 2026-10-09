import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../models/event_model.dart';
import '../../providers/application_provider.dart';
import '../../providers/auth_provider.dart';
import '../../theme/app_theme.dart';

class EventDetailScreen extends StatefulWidget {
  final EventModel event;

  const EventDetailScreen({super.key, required this.event});

  @override
  State<EventDetailScreen> createState() => _EventDetailScreenState();
}

class _EventDetailScreenState extends State<EventDetailScreen> {
  final _notesController = TextEditingController();
  String? _selectedRoleId;
  bool _isSubmitting = false;

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  void _showApplicationBottomSheet() {
    final event = widget.event;
    _notesController.clear();
    _selectedRoleId = event.roleRequirements.isNotEmpty ? event.roleRequirements.first.id : null;

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => StatefulBuilder(
        builder: (context, setModalState) {
          return Padding(
            padding: EdgeInsets.only(
              left: 24,
              right: 24,
              top: 20,
              bottom: MediaQuery.of(context).viewInsets.bottom + 24,
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Center(
                  child: Container(
                    width: 44,
                    height: 5,
                    decoration: BoxDecoration(
                      color: Colors.grey.shade300,
                      borderRadius: BorderRadius.circular(3),
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                Row(
                  children: [
                    const Icon(Icons.assignment_turned_in_rounded, color: AppTheme.primaryColor, size: 24),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        'Apply to Volunteer: ${event.title}',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w700,
                          color: AppTheme.textPrimary,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // Role Selection Dropdown (if roles exist)
                if (event.roleRequirements.isNotEmpty) ...[
                  const Text(
                    'Preferred Volunteer Role',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: AppTheme.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14),
                    decoration: BoxDecoration(
                      border: Border.all(color: AppTheme.borderSubtle),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        value: _selectedRoleId,
                        isExpanded: true,
                        items: event.roleRequirements.map((r) {
                          return DropdownMenuItem<String>(
                            value: r.id,
                            child: Text(
                              '${r.roleName} (${r.minExperienceLevel})',
                              style: const TextStyle(fontSize: 14),
                            ),
                          );
                        }).toList(),
                        onChanged: (val) {
                          setModalState(() {
                            _selectedRoleId = val;
                          });
                        },
                      ),
                    ),
                  ),
                  const SizedBox(height: 14),
                ],

                // Notes Field
                const Text(
                  'Application Notes & Availability',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: AppTheme.textSecondary,
                  ),
                ),
                const SizedBox(height: 6),
                TextField(
                  controller: _notesController,
                  maxLines: 3,
                  decoration: const InputDecoration(
                    hintText: 'Share relevant background, availability hours, or preferences...',
                  ),
                ),
                const SizedBox(height: 20),

                // Submit Button
                ElevatedButton(
                  onPressed: _isSubmitting
                      ? null
                      : () async {
                          setModalState(() => _isSubmitting = true);
                          final messenger = ScaffoldMessenger.of(context);
                          final appProvider = Provider.of<ApplicationProvider>(context, listen: false);

                          final success = await appProvider.applyForEvent(
                            eventId: event.id,
                            roleRequirementId: _selectedRoleId,
                            notes: _notesController.text.trim(),
                          );

                          setModalState(() => _isSubmitting = false);
                          if (ctx.mounted) {
                            Navigator.pop(ctx); // Close bottom sheet
                          }
                          if (!mounted) return;

                          if (success) {
                            _showSuccessConfirmationDialog();
                          } else {
                            final error = appProvider.errorMessage ?? 'Application could not be submitted.';
                            messenger.showSnackBar(
                              SnackBar(
                                content: Text(error),
                                backgroundColor: AppTheme.statusRejected,
                                behavior: SnackBarBehavior.floating,
                              ),
                            );
                          }
                        },
                  child: _isSubmitting
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Confirm & Submit Application'),
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  /// Official receipt confirmation dialog (Requirement 6 & Section 11 Integration)
  void _showSuccessConfirmationDialog() {
    final user = Provider.of<AuthProvider>(context, listen: false).currentUser;

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: const Row(
          children: [
            Icon(Icons.mark_email_read_rounded, color: AppTheme.statusApproved, size: 28),
            SizedBox(width: 10),
            Expanded(
              child: Text(
                'Application Submitted!',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18.5),
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'A confirmation receipt has been sent to your registered email address.',
              style: TextStyle(
                fontSize: 14.5,
                fontWeight: FontWeight.w600,
                color: AppTheme.textPrimary,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppTheme.statusApprovedBg,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Row(
                children: [
                  const Icon(Icons.email_outlined, color: AppTheme.statusApproved, size: 18),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Sent to: ${user?.email ?? "your email"}',
                      style: const TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w600,
                        color: AppTheme.statusApproved,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            const Text(
              'Your application status is now "Submitted". You can monitor organizer reviews and approvals in the "My Applications" tab.',
              style: TextStyle(fontSize: 13, color: AppTheme.textSecondary),
            ),
          ],
        ),
        actions: [
          ElevatedButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              Navigator.of(context).pop(); // Return to events list
            },
            child: const Text('View My Applications'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final event = widget.event;
    final appProvider = Provider.of<ApplicationProvider>(context);
    final isAlreadyApplied = appProvider.hasApplied(event.id);

    final dateFormat = DateFormat('EEEE, MMMM d, yyyy');
    final formattedStart = dateFormat.format(event.startDate);
    final formattedEnd = dateFormat.format(event.endDate);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Event Details'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Category & Status Row
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppTheme.primaryLight,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    event.category.toUpperCase(),
                    style: const TextStyle(
                      color: AppTheme.primaryColor,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.5,
                    ),
                  ),
                ),
                const Spacer(),
                if (isAlreadyApplied)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: AppTheme.statusApprovedBg,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Row(
                      children: [
                        Icon(Icons.check_circle, size: 14, color: AppTheme.statusApproved),
                        SizedBox(width: 4),
                        Text(
                          'Applied',
                          style: TextStyle(
                            color: AppTheme.statusApproved,
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 12),

            // Event Title
            Text(
              event.title,
              style: const TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.w800,
                color: AppTheme.textPrimary,
                letterSpacing: -0.5,
              ),
            ),
            const SizedBox(height: 16),

            // Logistics info cards
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: AppTheme.primaryLight,
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: const Icon(Icons.location_on, color: AppTheme.primaryColor, size: 20),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Venue', style: TextStyle(fontSize: 12, color: AppTheme.textSecondary)),
                              Text(
                                event.venueName ?? 'Convention Center Arena',
                                style: const TextStyle(fontSize: 14.5, fontWeight: FontWeight.w700),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const Divider(height: 20),
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: const Color(0xFFF3E8FF),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: const Icon(Icons.date_range, color: AppTheme.accentColor, size: 20),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Event Dates', style: TextStyle(fontSize: 12, color: AppTheme.textSecondary)),
                              Text(
                                '$formattedStart to $formattedEnd',
                                style: const TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 20),

            // Event Description
            const Text(
              'About the Event',
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w700,
                color: AppTheme.textPrimary,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              event.description.isNotEmpty
                  ? event.description
                  : 'Join us for this premier volunteer event. Your contribution will directly support attendees, operations, and community outreach.',
              style: const TextStyle(
                fontSize: 14,
                color: AppTheme.textSecondary,
                height: 1.5,
              ),
            ),
            const SizedBox(height: 24),

            // Required Volunteer Roles
            if (event.roleRequirements.isNotEmpty) ...[
              const Text(
                'Available Roles & Headcounts',
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.textPrimary,
                ),
              ),
              const SizedBox(height: 10),
              ...event.roleRequirements.map((r) {
                return Card(
                  margin: const EdgeInsets.only(bottom: 10),
                  child: Padding(
                    padding: const EdgeInsets.all(14),
                    child: Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: AppTheme.primaryLight,
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: const Icon(Icons.badge_outlined, color: AppTheme.primaryColor, size: 20),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                r.roleName,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                  fontSize: 14.5,
                                  color: AppTheme.textPrimary,
                                ),
                              ),
                              if (r.description != null && r.description!.isNotEmpty)
                                Text(
                                  r.description!,
                                  style: const TextStyle(fontSize: 12, color: AppTheme.textSecondary),
                                ),
                            ],
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: const Color(0xFFF1F5F9),
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: Text(
                            '${r.requiredHeadcount} Needed',
                            style: const TextStyle(
                              fontSize: 11.5,
                              fontWeight: FontWeight.w600,
                              color: AppTheme.textSecondary,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                );
              }),
            ],
            const SizedBox(height: 24),

            // Prominent Apply Button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: isAlreadyApplied ? null : _showApplicationBottomSheet,
                icon: Icon(
                  isAlreadyApplied ? Icons.check_circle_outline : Icons.send_rounded,
                  size: 20,
                ),
                label: Text(
                  isAlreadyApplied ? 'Application Already Submitted' : 'Apply to Volunteer',
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
                ),
              ),
            ),
            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }
}
