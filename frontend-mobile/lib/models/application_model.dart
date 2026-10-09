class ApplicationModel {
  final String id;
  final String eventId;
  final String volunteerId;
  final String? roleRequirementId;
  final String status;
  final String? notes;
  final DateTime appliedAt;
  final DateTime? reviewedAt;
  final String eventTitle;
  final String? venueName;
  final DateTime? eventStartDate;
  final DateTime? eventEndDate;
  final String? roleName;

  ApplicationModel({
    required this.id,
    required this.eventId,
    required this.volunteerId,
    this.roleRequirementId,
    required this.status,
    this.notes,
    required this.appliedAt,
    this.reviewedAt,
    required this.eventTitle,
    this.venueName,
    this.eventStartDate,
    this.eventEndDate,
    this.roleName,
  });

  factory ApplicationModel.fromJson(Map<String, dynamic> json) {
    DateTime applied = DateTime.tryParse(json['appliedAt']?.toString() ?? '') ?? DateTime.now();
    DateTime? reviewed = json['reviewedAt'] != null
        ? DateTime.tryParse(json['reviewedAt'].toString())
        : null;
    DateTime? start = json['eventStartDate'] != null
        ? DateTime.tryParse(json['eventStartDate'].toString())
        : null;
    DateTime? end = json['eventEndDate'] != null
        ? DateTime.tryParse(json['eventEndDate'].toString())
        : null;

    return ApplicationModel(
      id: json['id']?.toString() ?? '',
      eventId: json['eventId']?.toString() ?? '',
      volunteerId: json['volunteerId']?.toString() ?? '',
      roleRequirementId: json['roleRequirementId']?.toString(),
      status: json['status']?.toString() ?? 'Submitted',
      notes: json['notes']?.toString(),
      appliedAt: applied,
      reviewedAt: reviewed,
      eventTitle: json['eventTitle']?.toString() ?? 'Event Application',
      venueName: json['venueName']?.toString() ?? 'Convention Center',
      eventStartDate: start,
      eventEndDate: end,
      roleName: json['roleName']?.toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'eventId': eventId,
      'volunteerId': volunteerId,
      'roleRequirementId': roleRequirementId,
      'status': status,
      'notes': notes,
      'appliedAt': appliedAt.toIso8601String(),
      'reviewedAt': reviewedAt?.toIso8601String(),
      'eventTitle': eventTitle,
      'venueName': venueName,
      'eventStartDate': eventStartDate?.toIso8601String(),
      'eventEndDate': eventEndDate?.toIso8601String(),
      'roleName': roleName,
    };
  }
}
