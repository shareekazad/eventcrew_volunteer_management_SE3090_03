import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

/// Reusable status badge representing live application status progression (SE3090 Section 7).
class StatusBadge extends StatelessWidget {
  final String status;

  const StatusBadge({super.key, required this.status});

  @override
  Widget build(BuildContext context) {
    Color bg;
    Color textColor;
    IconData icon;
    String label = status;

    switch (status.toLowerCase()) {
      case 'submitted':
        bg = AppTheme.statusSubmittedBg;
        textColor = AppTheme.statusSubmitted;
        icon = Icons.hourglass_top_rounded;
        label = 'Submitted';
        break;
      case 'underreview':
      case 'under review':
        bg = AppTheme.statusUnderReviewBg;
        textColor = AppTheme.statusUnderReview;
        icon = Icons.search_rounded;
        label = 'Under Review';
        break;
      case 'shortlisted':
        bg = const Color(0xFFF3E8FF);
        textColor = const Color(0xFF7E22CE);
        icon = Icons.star_rounded;
        label = 'Shortlisted';
        break;
      case 'accepted':
      case 'approved':
        bg = AppTheme.statusApprovedBg;
        textColor = AppTheme.statusApproved;
        icon = Icons.check_circle_rounded;
        label = 'Approved / Accepted!';
        break;
      case 'rejected':
        bg = AppTheme.statusRejectedBg;
        textColor = AppTheme.statusRejected;
        icon = Icons.cancel_rounded;
        label = 'Rejected';
        break;
      default:
        bg = const Color(0xFFF1F5F9);
        textColor = const Color(0xFF475569);
        icon = Icons.info_outline_rounded;
        label = status;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: textColor.withValues(alpha: 0.3), width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: textColor),
          const SizedBox(width: 5),
          Text(
            label,
            style: TextStyle(
              color: textColor,
              fontSize: 12,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}
