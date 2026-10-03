/// A confirmed shift assignment owned by the authenticated volunteer.
class ShiftAssignmentModel {
  const ShiftAssignmentModel({
    required this.assignmentId,
    required this.shiftId,
    required this.title,
    required this.eventName,
    required this.roleRequirementName,
    required this.startTime,
    required this.endTime,
    required this.status,
  });

  final String assignmentId;
  final String shiftId;
  final String title;
  final String eventName;
  final String roleRequirementName;
  final DateTime startTime;
  final DateTime endTime;
  final String status;

  factory ShiftAssignmentModel.fromJson(Map<String, dynamic> json) {
    String requiredString(String key) {
      final value = json[key];
      if (value is String && value.isNotEmpty) return value;
      throw FormatException(
        'Missing or invalid "$key" in assignment response.',
      );
    }

    DateTime requiredDateTime(String key) {
      final value = json[key];
      if (value is String) {
        final parsed = DateTime.tryParse(value);
        if (parsed != null) return parsed;
      }
      throw FormatException(
        'Missing or invalid "$key" in assignment response.',
      );
    }

    return ShiftAssignmentModel(
      assignmentId: requiredString('assignmentId'),
      shiftId: requiredString('shiftId'),
      title: requiredString('title'),
      eventName: requiredString('eventName'),
      roleRequirementName: requiredString('roleRequirementName'),
      startTime: requiredDateTime('startTime'),
      endTime: requiredDateTime('endTime'),
      status: requiredString('status'),
    );
  }
}

/// Volunteer-owned confirmed assignments returned by `/api/shifts/my-assignments`.
class VolunteerAssignmentsModel {
  const VolunteerAssignmentsModel({
    required this.volunteerId,
    required this.assignments,
  });

  final String volunteerId;
  final List<ShiftAssignmentModel> assignments;

  factory VolunteerAssignmentsModel.fromJson(Map<String, dynamic> json) {
    final volunteerId = json['volunteerId'];
    final rawAssignments = json['assignments'];
    if (volunteerId is! String || volunteerId.isEmpty) {
      throw const FormatException(
        'Missing or invalid "volunteerId" in assignments response.',
      );
    }
    if (rawAssignments is! List) {
      throw const FormatException(
        'Missing or invalid "assignments" in assignments response.',
      );
    }

    return VolunteerAssignmentsModel(
      volunteerId: volunteerId,
      assignments: rawAssignments
          .map(
            (item) =>
                ShiftAssignmentModel.fromJson(item as Map<String, dynamic>),
          )
          .toList(),
    );
  }
}
