import 'package:flutter/material.dart';

import '../../../events/data/models/event_model.dart';
import '../../../events/data/repositories/event_repository.dart';
import '../../data/models/application_model.dart';
import '../../data/repositories/application_repository.dart';

/// "My Applications" screen — shows all events the volunteer has applied to,
/// with the current status of each application.
///
/// Loads:
/// - The volunteer's applications from GET /api/applications/mine
/// - The event details for each application (to show the event title)
class MyApplicationsScreen extends StatefulWidget {
  const MyApplicationsScreen({super.key});

  @override
  State<MyApplicationsScreen> createState() => _MyApplicationsScreenState();
}

class _MyApplicationsScreenState extends State<MyApplicationsScreen> {
  final _applicationRepository = ApplicationRepository();
  final _eventRepository = EventRepository();

  bool _isLoading = true;
  String? _errorMessage;
  List<ApplicationModel> _applications = [];
  final Map<String, EventModel?> _eventCache = {};

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
      final applications = await _applicationRepository.getMyApplications();

      // Fetch event details for each application (cached)
      for (final app in applications) {
        if (!_eventCache.containsKey(app.eventId)) {
          try {
            final event = await _eventRepository.getEventById(app.eventId);
            _eventCache[app.eventId] = event;
          } catch (_) {
            _eventCache[app.eventId] = null;
          }
        }
      }

      if (!mounted) return;
      setState(() {
        _applications = applications;
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
        title: const Text('My Applications'),
        actions: [
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
      return _buildErrorState();
    }

    if (_applications.isEmpty) {
      return _buildEmptyState();
    }

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.separated(
        padding: const EdgeInsets.all(12),
        itemCount: _applications.length,
        separatorBuilder: (_, __) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final app = _applications[index];
          final event = _eventCache[app.eventId];
          return _ApplicationCard(app: app, event: event);
        },
      ),
    );
  }

  Widget _buildErrorState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 64, color: Colors.redAccent),
            const SizedBox(height: 16),
            const Text(
              'Could not load applications',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              _errorMessage ?? 'Unknown error',
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.black54),
            ),
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

  Widget _buildEmptyState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.description_outlined,
                size: 64, color: Colors.grey),
            const SizedBox(height: 16),
            const Text(
              'No applications yet',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Browse events and tap "Apply" to submit your first application.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.black54),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: () => Navigator.of(context).maybePop(),
              icon: const Icon(Icons.explore_outlined),
              label: const Text('Discover Events'),
            ),
          ],
        ),
      ),
    );
  }
}

// ============================================================================
// Application card widget
// ============================================================================
class _ApplicationCard extends StatelessWidget {
  const _ApplicationCard({required this.app, this.event});

  final ApplicationModel app;
  final EventModel? event;

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 1,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    event?.title ?? 'Event',
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                _StatusBadge(status: app.status, label: app.statusLabel),
              ],
            ),
            const SizedBox(height: 8),
            if (event != null) ...[
              Row(
                children: [
                  const Icon(Icons.category_outlined,
                      size: 14, color: Colors.black54),
                  const SizedBox(width: 4),
                  Text(
                    event!.category,
                    style: const TextStyle(fontSize: 13, color: Colors.black54),
                  ),
                  const SizedBox(width: 12),
                  const Icon(Icons.calendar_today_outlined,
                      size: 14, color: Colors.black54),
                  const SizedBox(width: 4),
                  Text(
                    _formatDate(event!.startDate),
                    style: const TextStyle(fontSize: 13, color: Colors.black54),
                  ),
                ],
              ),
              const SizedBox(height: 10),
            ],
            Row(
              children: [
                const Icon(Icons.schedule, size: 14, color: Colors.black54),
                const SizedBox(width: 4),
                Text(
                  'Applied ${_formatDate(app.appliedAt)}',
                  style: const TextStyle(fontSize: 12, color: Colors.black54),
                ),
              ],
            ),
            if (app.notes != null && app.notes!.isNotEmpty) ...[
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: Colors.grey.shade100,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  app.notes!,
                  style: const TextStyle(fontSize: 13, color: Colors.black87),
                ),
              ),
            ],
            if (app.reviewedAt != null) ...[
              const SizedBox(height: 8),
              Text(
                'Reviewed ${_formatDate(app.reviewedAt!)}',
                style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _formatDate(DateTime d) {
    return '${d.day} ${_monthName(d.month)} ${d.year}';
  }

  String _monthName(int m) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    return months[m - 1];
  }
}

// ============================================================================
// Status badge widget
// ============================================================================
class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.status, required this.label});

  final String status;
  final String label;

  @override
  Widget build(BuildContext context) {
    final (bg, fg) = _colors(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: fg,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  (Color, Color) _colors(String status) {
    switch (status) {
      case 'Submitted':
        return (Colors.blue.shade50, Colors.blue.shade700);
      case 'UnderReview':
        return (Colors.amber.shade50, Colors.amber.shade800);
      case 'Shortlisted':
        return (Colors.purple.shade50, Colors.purple.shade700);
      case 'Accepted':
        return (Colors.green.shade50, Colors.green.shade700);
      case 'Rejected':
        return (Colors.red.shade50, Colors.red.shade700);
      default:
        return (Colors.grey.shade200, Colors.grey.shade700);
    }
  }
}