import 'package:flutter/material.dart';

import '../../../applications/data/models/application_model.dart';
import '../../../applications/data/repositories/application_repository.dart';
import '../../data/models/event_model.dart';
import '../../data/models/role_requirement_model.dart';
import '../../data/repositories/event_repository.dart';

/// Detailed view of a single event.
///
/// Fetches the event from GET /api/Events/{id} on open.
/// Volunteers can submit an application via the "Apply to Volunteer"
/// button at the bottom of the screen — but only when the event is open
/// for applications (Published / StaffingInProgress).
class EventDetailScreen extends StatefulWidget {
  final String eventId;

  const EventDetailScreen({super.key, required this.eventId});

  @override
  State<EventDetailScreen> createState() => _EventDetailScreenState();
}

class _EventDetailScreenState extends State<EventDetailScreen> {
  final EventRepository _eventRepository = EventRepository();
  final ApplicationRepository _applicationRepository = ApplicationRepository();

  bool _isLoading = true;
  String? _errorMessage;
  EventModel? _event;

  // ---- Application state ----
  bool _checkingApplication = true;
  ApplicationModel? _existingApplication; // null = never applied

  @override
  void initState() {
    super.initState();
    _loadEvent();
  }

  Future<void> _loadEvent() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final event = await _eventRepository.getEventById(widget.eventId);
      if (!mounted) return;
      setState(() {
        _event = event;
        _isLoading = false;
        if (event == null) {
          _errorMessage = 'Event not found.';
        }
      });

      // Now check if the volunteer already applied to this event.
      if (event != null) {
        await _checkExistingApplication();
      }
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString();
        _isLoading = false;
      });
    }
  }

  Future<void> _checkExistingApplication() async {
    setState(() => _checkingApplication = true);
    try {
      final applications = await _applicationRepository.getMyApplications();
      final match = applications
          .cast<ApplicationModel?>()
          .firstWhere(
            (a) => a?.eventId == widget.eventId,
            orElse: () => null,
          );
      if (!mounted) return;
      setState(() {
        _existingApplication = match;
        _checkingApplication = false;
      });
    } catch (_) {
      // If we can't fetch, treat as "not applied" — the Apply button
      // will still return a clear error if the user tries again.
      if (!mounted) return;
      setState(() {
        _existingApplication = null;
        _checkingApplication = false;
      });
    }
  }

  /// Only allow applying when the event is accepting applications.
  bool get _canApply {
    if (_event == null) return false;
    if (_existingApplication != null) return false;
    if (_checkingApplication) return false;
    return _event!.status == 'Published' ||
        _event!.status == 'StaffingInProgress';
  }

  Future<void> _showApplyDialog() async {
    final notesController = TextEditingController();
    String? selectedRoleId;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: const Text('Apply to volunteer'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'You can add a short note to introduce yourself. '
                  'The organizer will see this when reviewing applications.',
                  style: TextStyle(fontSize: 13, color: Colors.black54),
                ),
                const SizedBox(height: 16),

                // Optional role selection
                if (_event!.roleRequirements.isNotEmpty) ...[
                  const Text(
                    'Preferred role (optional)',
                    style: TextStyle(fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 4),
                  DropdownButtonFormField<String?>(
                    initialValue: selectedRoleId,
                    decoration: const InputDecoration(
                      border: OutlineInputBorder(),
                      isDense: true,
                    ),
                    items: [
                      const DropdownMenuItem<String?>(
                        value: null,
                        child: Text('Any role'),
                      ),
                      ..._event!.roleRequirements.map(
                        (role) => DropdownMenuItem<String?>(
                          value: role.id,
                          child: Text(role.roleName),
                        ),
                      ),
                    ],
                    onChanged: (v) => setState(() => selectedRoleId = v),
                  ),
                  const SizedBox(height: 16),
                ],

                const Text(
                  'Motivation (optional)',
                  style: TextStyle(fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 4),
                TextField(
                  controller: notesController,
                  maxLines: 3,
                  maxLength: 2000,
                  decoration: const InputDecoration(
                    border: OutlineInputBorder(),
                    hintText: 'Why do you want to volunteer?',
                  ),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.of(ctx).pop(true),
              child: const Text('Submit Application'),
            ),
          ],
        ),
      ),
    );

    if (confirmed != true) return;

    // ---- Submit application ----
    try {
      final app = await _applicationRepository.applyToEvent(
        eventId: widget.eventId,
        roleRequirementId: selectedRoleId,
        notes: notesController.text.trim().isEmpty
            ? null
            : notesController.text.trim(),
      );

      if (!mounted) return;
      setState(() => _existingApplication = app);

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Application submitted! Check "My Applications" for status.'),
          backgroundColor: Colors.green,
        ),
      );
    } catch (e) {
      if (!mounted) return;
      final message = e.toString();

      // 409 conflict = already applied
      if (message.contains('409') || message.contains('already applied')) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('You have already applied to this event.'),
            backgroundColor: Colors.orange,
          ),
        );
        await _checkExistingApplication();
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Could not apply: $message'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Event Details')),
      body: _buildBody(),
      bottomNavigationBar: _buildBottomBar(),
    );
  }

  /// The bottom apply bar, shown only when the event is loaded.
  Widget? _buildBottomBar() {
    if (_isLoading || _event == null || _errorMessage != null) return null;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: _checkingApplication
            ? const SizedBox(
                height: 52,
                child: Center(child: CircularProgressIndicator()),
              )
            : _existingApplication != null
                ? _buildAlreadyAppliedBanner()
                : _buildApplyButton(),
      ),
    );
  }

  Widget _buildApplyButton() {
    final enabled = _canApply;
    return SizedBox(
      height: 52,
      child: FilledButton.icon(
        onPressed: enabled ? _showApplyDialog : null,
        icon: const Icon(Icons.how_to_reg),
        label: Text(
          enabled
              ? 'Apply to Volunteer'
              : 'Applications closed for this event',
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }

  Widget _buildAlreadyAppliedBanner() {
    return Container(
      height: 52,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      decoration: BoxDecoration(
        color: Colors.green.shade50,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.green.shade200),
      ),
      child: Row(
        children: [
          Icon(Icons.check_circle, color: Colors.green.shade700),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Already applied',
                  style: TextStyle(
                    color: Colors.green.shade800,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                Text(
                  'Status: ${_existingApplication!.statusLabel}',
                  style: TextStyle(
                    color: Colors.green.shade700,
                    fontSize: 12,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
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
              const Icon(Icons.error_outline, size: 64, color: Colors.redAccent),
              const SizedBox(height: 16),
              Text(_errorMessage!, textAlign: TextAlign.center),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: _loadEvent,
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    final event = _event!;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            event.title,
            style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          _StatusChip(status: event.status),
          const SizedBox(height: 16),

          _InfoRow(
            icon: Icons.category_outlined,
            label: 'Category',
            value: event.category,
          ),
          const SizedBox(height: 8),

          _InfoRow(
            icon: Icons.event,
            label: 'Starts',
            value: _formatDateTime(event.startDate),
          ),
          const SizedBox(height: 8),

          _InfoRow(
            icon: Icons.event_available,
            label: 'Ends',
            value: _formatDateTime(event.endDate),
          ),
          const SizedBox(height: 24),

          if (event.description != null && event.description!.isNotEmpty) ...[
            const Text(
              'About',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              event.description!,
              style: const TextStyle(fontSize: 15, height: 1.5),
            ),
            const SizedBox(height: 24),
          ],

          const Text(
            'Roles Needed',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          if (event.roleRequirements.isEmpty)
            const Text(
              'No role requirements yet.',
              style: TextStyle(color: Colors.black54),
            )
          else
            ...event.roleRequirements.map((role) => _RoleTile(role: role)),

          const SizedBox(height: 32),
        ],
      ),
    );
  }

  String _formatDateTime(DateTime dt) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    final h = dt.hour.toString().padLeft(2, '0');
    final m = dt.minute.toString().padLeft(2, '0');
    return '${dt.day} ${months[dt.month - 1]} ${dt.year} at $h:$m';
  }
}

/// A single role requirement row.
class _RoleTile extends StatelessWidget {
  final RoleRequirementModel role;

  const _RoleTile({required this.role});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: const CircleAvatar(child: Icon(Icons.badge_outlined)),
        title: Text(role.roleName),
        subtitle: Text(
          '${role.requiredHeadcount} needed • ${role.minExperienceLevel}',
        ),
        trailing: role.description != null
            ? Tooltip(
                message: role.description!,
                child: const Icon(Icons.info_outline),
              )
            : null,
      ),
    );
  }
}

/// A row showing an icon + label + value.
class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 18, color: Colors.black54),
        const SizedBox(width: 8),
        Text('$label: ', style: const TextStyle(fontWeight: FontWeight.w600)),
        Expanded(child: Text(value)),
      ],
    );
  }
}

/// Colored status chip.
class _StatusChip extends StatelessWidget {
  final String status;

  const _StatusChip({required this.status});

  @override
  Widget build(BuildContext context) {
    final color = _colorFor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: color.withValues(alpha: 0.4)),
      ),
      child: Text(
        status,
        style: TextStyle(color: color, fontWeight: FontWeight.w600),
      ),
    );
  }

  Color _colorFor(String status) {
    switch (status) {
      case 'Draft':
        return Colors.grey;
      case 'Published':
        return Colors.blue;
      case 'StaffingInProgress':
        return Colors.orange;
      case 'FullyStaffed':
        return Colors.green;
      case 'Completed':
        return Colors.teal;
      case 'Cancelled':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }
}