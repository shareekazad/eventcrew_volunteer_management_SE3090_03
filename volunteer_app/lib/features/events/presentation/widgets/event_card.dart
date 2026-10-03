import 'package:flutter/material.dart';

import '../../data/models/event_model.dart';

/// A card that displays a single event in the discovery feed.
///
/// Reusable widget — takes an [EventModel] and an [onTap] callback.
/// The card itself has no navigation logic — the parent decides what happens on tap.
class EventCard extends StatelessWidget {
  final EventModel event;
  final VoidCallback onTap;

  const EventCard({
    super.key,
    required this.event,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      elevation: 2,
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Text(
                      event.title,
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  const SizedBox(width: 8),
                  _StatusBadge(status: event.status),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  const Icon(Icons.category_outlined,
                      size: 16, color: Colors.black54),
                  const SizedBox(width: 4),
                  Text(
                    event.category,
                    style: const TextStyle(color: Colors.black54),
                  ),
                  const SizedBox(width: 12),
                  const Icon(Icons.calendar_today_outlined,
                      size: 16, color: Colors.black54),
                  const SizedBox(width: 4),
                  Text(
                    event.formattedDate,
                    style: const TextStyle(color: Colors.black54),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              if (event.description != null && event.description!.isNotEmpty) ...[
                Text(
                  event.description!,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: Colors.black87),
                ),
                const SizedBox(height: 12),
              ],
              Row(
                children: [
                  const Icon(Icons.groups_outlined,
                      size: 16, color: Colors.black54),
                  const SizedBox(width: 4),
                  Text(
                    '${event.roleRequirements.length} roles • '
                    '${event.totalRequiredStaff} volunteers needed',
                    style: const TextStyle(
                      color: Colors.black54,
                      fontSize: 13,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Small colored badge showing the event status.
/// Private to this file — only used by EventCard.
class _StatusBadge extends StatelessWidget {
  final String status;

  const _StatusBadge({required this.status});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: _colorFor(status).withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: _colorFor(status).withValues(alpha: 0.4)),
      ),
      child: Text(
        status,
        style: TextStyle(
          color: _colorFor(status),
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
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