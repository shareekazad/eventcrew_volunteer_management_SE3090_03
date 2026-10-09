import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/event_model.dart';
import '../../providers/event_provider.dart';
import '../../theme/app_theme.dart';
import '../../widgets/event_card.dart';
import 'event_detail_screen.dart';

class EventsScreen extends StatefulWidget {
  const EventsScreen({super.key});

  @override
  State<EventsScreen> createState() => _EventsScreenState();
}

class _EventsScreenState extends State<EventsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      Provider.of<EventProvider>(context, listen: false).fetchEvents();
    });
  }

  @override
  Widget build(BuildContext context) {
    final eventProvider = Provider.of<EventProvider>(context);

    return RefreshIndicator(
      onRefresh: () => eventProvider.fetchEvents(),
      color: AppTheme.primaryColor,
      child: eventProvider.isLoading && eventProvider.events.isEmpty
          ? const Center(child: CircularProgressIndicator())
          : eventProvider.errorMessage != null && eventProvider.events.isEmpty
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.cloud_off_rounded, size: 48, color: AppTheme.textMuted),
                        const SizedBox(height: 12),
                        Text(
                          eventProvider.errorMessage!,
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: AppTheme.textSecondary, fontSize: 14),
                        ),
                        const SizedBox(height: 16),
                        ElevatedButton.icon(
                          onPressed: () => eventProvider.fetchEvents(),
                          icon: const Icon(Icons.refresh, size: 18),
                          label: const Text('Try Again'),
                        ),
                      ],
                    ),
                  ),
                )
              : eventProvider.events.isEmpty
                  ? const Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.event_busy, size: 52, color: AppTheme.textMuted),
                          SizedBox(height: 12),
                          Text(
                            'No published events right now.',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w600,
                              color: AppTheme.textPrimary,
                            ),
                          ),
                          SizedBox(height: 6),
                          Text(
                            'Check back soon or pull to refresh!',
                            style: TextStyle(fontSize: 13, color: AppTheme.textSecondary),
                          ),
                        ],
                      ),
                    )
                  : ListView.separated(
                      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
                      itemCount: eventProvider.events.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 14),
                      itemBuilder: (ctx, index) {
                        final EventModel event = eventProvider.events[index];
                        return EventCard(
                          event: event,
                          onTap: () {
                            Navigator.of(context).push(
                              MaterialPageRoute(
                                builder: (_) => EventDetailScreen(event: event),
                              ),
                            );
                          },
                        );
                      },
                    ),
    );
  }
}
