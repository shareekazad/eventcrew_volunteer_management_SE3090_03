/// Dart model for a volunteer's application to an event.
///
/// Mirrors the backend's `ApplicationResponseDto`.
class ApplicationModel {
  final String id;
  final String eventId;
  final String volunteerId;
  final String? roleRequirementId;
  final String status; // Submitted | UnderReview | Shortlisted | Accepted | Rejected
  final String? notes;
  final DateTime appliedAt;
  final DateTime? reviewedAt;

  const ApplicationModel({
    required this.id,
    required this.eventId,
    required this.volunteerId,
    this.roleRequirementId,
    required this.status,
    this.notes,
    required this.appliedAt,
    this.reviewedAt,
  });

  factory ApplicationModel.fromJson(Map<String, dynamic> json) {
    return ApplicationModel(
      id: json['id'] as String,
      eventId: json['eventId'] as String,
      volunteerId: json['volunteerId'] as String,
      roleRequirementId: json['roleRequirementId'] as String?,
      status: json['status'] as String? ?? 'Submitted',
      notes: json['notes'] as String?,
      appliedAt: _parseDate(json['appliedAt']) ?? DateTime.now(),
      reviewedAt: _parseDate(json['reviewedAt']),
    );
  }

  /// Human-readable label for the status.
  String get statusLabel {
    switch (status) {
      case 'Submitted':
        return 'Submitted';
      case 'UnderReview':
        return 'Under Review';
      case 'Shortlisted':
        return 'Shortlisted';
      case 'Accepted':
        return 'Accepted';
      case 'Rejected':
        return 'Not Selected';
      default:
        return status;
    }
  }

  static DateTime? _parseDate(dynamic raw) {
    if (raw == null) return null;
    if (raw is String) return DateTime.tryParse(raw)?.toLocal();
    return null;
  }
}