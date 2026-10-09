import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../models/application_model.dart';
import '../../providers/application_provider.dart';
import '../../theme/app_theme.dart';
import '../../widgets/status_badge.dart';

class MyApplicationsScreen extends StatefulWidget {
  final VoidCallback onBrowseEvents;

  const MyApplicationsScreen({super.key, required this.onBrowseEvents});

  @override
  State<MyApplicationsScreen> createState() => _MyApplicationsScreenState();
}

class _MyApplicationsScreenState extends State<MyApplicationsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      Provider.of<ApplicationProvider>(context, listen: false).fetchMyApplications();
    });
  }

  @override
  Widget build(BuildContext context) {
    final appProvider = Provider.of<ApplicationProvider>(context);
    final dateFormat = DateFormat('MMM d, yyyy • h:mm a');

    return RefreshIndicator(
      onRefresh: () => appProvider.fetchMyApplications(),
      color: AppTheme.primaryColor,
      child: appProvider.isLoading && appProvider.applications.isEmpty
          ? const Center(child: CircularProgressIndicator())
          : appProvider.errorMessage != null && appProvider.applications.isEmpty
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.error_outline_rounded, size: 48, color: AppTheme.textMuted),
                        const SizedBox(height: 12),
                        Text(
                          appProvider.errorMessage!,
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: AppTheme.textSecondary, fontSize: 14),
                        ),
                        const SizedBox(height: 16),
                        ElevatedButton.icon(
                          onPressed: () => appProvider.fetchMyApplications(),
                          icon: const Icon(Icons.refresh, size: 18),
                          label: const Text('Refresh'),
                        ),
                      ],
                    ),
                  ),
                )
              : appProvider.applications.isEmpty
                  ? Center(
                      child: Padding(
                        padding: const EdgeInsets.all(32),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Container(
                              padding: const EdgeInsets.all(20),
                              decoration: const BoxDecoration(
                                color: AppTheme.primaryLight,
                                shape: BoxShape.circle,
                              ),
                              child: const Icon(
                                Icons.assignment_outlined,
                                size: 54,
                                color: AppTheme.primaryColor,
                              ),
                            ),
                            const SizedBox(height: 18),
                            const Text(
                              'No Applications Submitted Yet',
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.w700,
                                color: AppTheme.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 8),
                            const Text(
                              'Explore published events and submit your application to join the crew.',
                              textAlign: TextAlign.center,
                              style: TextStyle(fontSize: 13.5, color: AppTheme.textSecondary),
                            ),
                            const SizedBox(height: 20),
                            ElevatedButton.icon(
                              onPressed: widget.onBrowseEvents,
                              icon: const Icon(Icons.search, size: 18),
                              label: const Text('Explore Events Now'),
                            ),
                          ],
                        ),
                      ),
                    )
                  : ListView.separated(
                      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
                      itemCount: appProvider.applications.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 14),
                      itemBuilder: (ctx, index) {
                        final ApplicationModel application = appProvider.applications[index];

                        return Card(
                          child: Padding(
                            padding: const EdgeInsets.all(18),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                // Header: Event Title + Live Status Badge
                                Row(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Expanded(
                                      child: Text(
                                        application.eventTitle,
                                        style: const TextStyle(
                                          fontSize: 17,
                                          fontWeight: FontWeight.w700,
                                          color: AppTheme.textPrimary,
                                        ),
                                      ),
                                    ),
                                    const SizedBox(width: 8),
                                    // Live Color-Coded Status Badge (Requirement 7)
                                    StatusBadge(status: application.status),
                                  ],
                                ),
                                const SizedBox(height: 10),

                                // Venue and Role Row
                                if (application.venueName != null && application.venueName!.isNotEmpty)
                                  Padding(
                                    padding: const EdgeInsets.only(bottom: 6),
                                    child: Row(
                                      children: [
                                        const Icon(Icons.location_on_outlined, size: 15, color: AppTheme.primaryColor),
                                        const SizedBox(width: 5),
                                        Text(
                                          application.venueName!,
                                          style: const TextStyle(fontSize: 13, color: AppTheme.textPrimary, fontWeight: FontWeight.w500),
                                        ),
                                      ],
                                    ),
                                  ),

                                if (application.roleName != null && application.roleName!.isNotEmpty)
                                  Padding(
                                    padding: const EdgeInsets.only(bottom: 6),
                                    child: Row(
                                      children: [
                                        const Icon(Icons.work_outline, size: 15, color: AppTheme.textSecondary),
                                        const SizedBox(width: 5),
                                        Text(
                                          'Applied Role: ${application.roleName}',
                                          style: const TextStyle(fontSize: 13, color: AppTheme.textSecondary),
                                        ),
                                      ],
                                    ),
                                  ),

                                // Timestamp
                                Row(
                                  children: [
                                    const Icon(Icons.schedule, size: 14, color: AppTheme.textMuted),
                                    const SizedBox(width: 5),
                                    Text(
                                      'Submitted: ${dateFormat.format(application.appliedAt.toLocal())}',
                                      style: const TextStyle(fontSize: 12, color: AppTheme.textSecondary),
                                    ),
                                  ],
                                ),

                                // Notes (if any)
                                if (application.notes != null && application.notes!.isNotEmpty) ...[
                                  const SizedBox(height: 12),
                                  Container(
                                    width: double.infinity,
                                    padding: const EdgeInsets.all(12),
                                    decoration: BoxDecoration(
                                      color: const Color(0xFFF8FAFC),
                                      borderRadius: BorderRadius.circular(10),
                                      border: Border.all(color: AppTheme.borderSubtle),
                                    ),
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        const Text(
                                          'Volunteer Submission Note:',
                                          style: TextStyle(
                                            fontSize: 11.5,
                                            fontWeight: FontWeight.w700,
                                            color: AppTheme.textSecondary,
                                          ),
                                        ),
                                        const SizedBox(height: 4),
                                        Text(
                                          application.notes!,
                                          style: const TextStyle(fontSize: 13, color: AppTheme.textPrimary),
                                        ),
                                      ],
                                    ),
                                  ),
                                ],

                                // Status Explanation info banner
                                const SizedBox(height: 14),
                                _buildStatusExplanation(application.status),
                              ],
                            ),
                          ),
                        );
                      },
                    ),
    );
  }

  Widget _buildStatusExplanation(String status) {
    String tip;
    Color color;

    switch (status.toLowerCase()) {
      case 'submitted':
        tip = 'Your application is awaiting initial organizer screening.';
        color = AppTheme.statusSubmitted;
        break;
      case 'underreview':
      case 'under review':
        tip = 'The event lead is currently reviewing your profile and skills.';
        color = AppTheme.statusUnderReview;
        break;
      case 'shortlisted':
        tip = 'You have been shortlisted for interview or roster allocation!';
        color = const Color(0xFF7E22CE);
        break;
      case 'accepted':
      case 'approved':
        tip = '🎉 Congratulations! You have been accepted for this event.';
        color = AppTheme.statusApproved;
        break;
      case 'rejected':
        tip = 'Application was not selected for this specific event.';
        color = AppTheme.statusRejected;
        break;
      default:
        tip = 'Status updated by event organizer.';
        color = AppTheme.textSecondary;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withOpacity(0.08),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          Icon(Icons.info_outline, size: 14, color: color),
          const SizedBox(width: 6),
          Expanded(
            child: Text(
              tip,
              style: TextStyle(fontSize: 11.5, color: color, fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }
}
