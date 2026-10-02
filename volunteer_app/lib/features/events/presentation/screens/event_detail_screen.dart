import 'package:flutter/material.dart';

import '../../data/models/event_model.dart';
import '../../data/models/role_requirement_model.dart';
import '../../data/repositories/event_repository.dart';

/// Detailed view of a single event.
///
/// Fetches the event from GET /api/Events/{id} on open.
class EventDetailScreen extends StatefulWidget {
  final String eventId;

  const EventDetailScreen({super.key, required this.eventId});

  @override
  State<EventDetailScreen> createState() => _EventDetailScreenState();
}

class _EventDetailScreenState extends State<EventDetailScreen> {
  final EventRepository _repository = EventRepository();

  bool _isLoading = true;
  String? _errorMessage;
  EventModel? _event;

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
      final event = await _repository.getEventById(widget.eventId);
      setState(() {
        _event = event;
        _isLoading = false;
        if (event == null) {
          _errorMessage = 'Event not found.';
        }
      });
    } catch (e) {
      setState(() {
        _errorMessage = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Event Details')),
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