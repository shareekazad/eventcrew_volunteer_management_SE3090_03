/// Dart model for a Shift, mirroring the backend's ShiftResponseDto.
class ShiftModel {
  final String id;
  final String eventId;
  final String roleRequirementId;
  final String title;
  final DateTime startTime;
  final DateTime endTime;
  final int capacity;
  final String status;
  final int assignedCount;
  final List<ShiftAssignmentModel> assignments;

  const ShiftModel({
    required this.id,
    required this.eventId,
    required this.roleRequirementId,
    required this.title,
    required this.startTime,
    required this.endTime,
    required this.capacity,
    required this.status,
    required this.assignedCount,
    required this.assignments,
  });

  factory ShiftModel.fromJson(Map<String, dynamic> json) {
    final rawAssignments = json['assignments'] as List<dynamic>? ?? const [];
    return ShiftModel(
      id: json['id'] as String,
      eventId: json['eventId'] as String,
      roleRequirementId: json['roleRequirementId'] as String,
      title: json['title'] as String? ?? '',
      startTime: DateTime.parse(json['startTime'] as String).toLocal(),
      endTime: DateTime.parse(json['endTime'] as String).toLocal(),
      capacity: (json['capacity'] as num?)?.toInt() ?? 0,
      status: json['status'] as String? ?? 'Scheduled',
      assignedCount: (json['assignedCount'] as num?)?.toInt() ?? 0,
      assignments: rawAssignments
          .map((a) => ShiftAssignmentModel.fromJson(a as Map<String, dynamic>))
          .toList(),
    );
  }

  /// True if the current user is assigned to this shift.
  bool hasAssignmentFor(String volunteerId) {
    return assignments.any((a) => a.volunteerId == volunteerId);
  }
}

class ShiftAssignmentModel {
  final String id;
  final String shiftId;
  final String volunteerId;
  final String? volunteerName;
  final String status;
  final DateTime assignedAt;

  const ShiftAssignmentModel({
    required this.id,
    required this.shiftId,
    required this.volunteerId,
    this.volunteerName,
    required this.status,
    required this.assignedAt,
  });

  factory ShiftAssignmentModel.fromJson(Map<String, dynamic> json) {
    return ShiftAssignmentModel(
      id: json['id'] as String,
      shiftId: json['shiftId'] as String,
      volunteerId: json['volunteerId'] as String,
      volunteerName: json['volunteerName'] as String?,
      status: json['status'] as String? ?? 'Proposed_By_AI',
      assignedAt: DateTime.tryParse(json['assignedAt'] as String? ?? '')
              ?.toLocal() ??
          DateTime.now(),
    );
  }
}