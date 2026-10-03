/// Data model representing a ShiftSwapRequest from ASP.NET Core ShiftSwapResponse.
class ShiftSwapModel {
  final String id;
  final String requesterAssignmentId;
  final String requesterVolunteerId;
  final String requesterName;
  final String requesterEmail;
  final String sourceShiftId;
  final String sourceShiftTitle;
  final String sourceEventId;
  final String sourceEventTitle;
  final String targetVolunteerId;
  final String targetVolunteerName;
  final String targetVolunteerEmail;
  final String targetShiftId;
  final String targetShiftTitle;
  final String targetEventId;
  final String targetEventTitle;
  final String? reason;
  final String status;
  final DateTime createdAt;
  final DateTime updatedAt;

  const ShiftSwapModel({
    required this.id,
    required this.requesterAssignmentId,
    required this.requesterVolunteerId,
    required this.requesterName,
    required this.requesterEmail,
    required this.sourceShiftId,
    required this.sourceShiftTitle,
    required this.sourceEventId,
    required this.sourceEventTitle,
    required this.targetVolunteerId,
    required this.targetVolunteerName,
    required this.targetVolunteerEmail,
    required this.targetShiftId,
    required this.targetShiftTitle,
    required this.targetEventId,
    required this.targetEventTitle,
    this.reason,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
  });

  factory ShiftSwapModel.fromJson(Map<String, dynamic> json) {
    String requiredString(String camelCase, String pascalCase) {
      final value = json[camelCase] ?? json[pascalCase];
      if (value is String && value.isNotEmpty) return value;
      throw FormatException(
        'Missing or invalid "$camelCase" in shift swap response.',
      );
    }

    DateTime requiredDateTime(String camelCase, String pascalCase) {
      final value = json[camelCase] ?? json[pascalCase];
      if (value is String) {
        final parsed = DateTime.tryParse(value);
        if (parsed != null) return parsed;
      }
      throw FormatException(
        'Missing or invalid "$camelCase" in shift swap response.',
      );
    }

    return ShiftSwapModel(
      id: requiredString('id', 'Id'),
      requesterAssignmentId: requiredString(
        'requesterAssignmentId',
        'RequesterAssignmentId',
      ),
      requesterVolunteerId: requiredString(
        'requesterVolunteerId',
        'RequesterVolunteerId',
      ),
      requesterName: requiredString('requesterName', 'RequesterName'),
      requesterEmail: requiredString('requesterEmail', 'RequesterEmail'),
      sourceShiftId: requiredString('sourceShiftId', 'SourceShiftId'),
      sourceShiftTitle: requiredString('sourceShiftTitle', 'SourceShiftTitle'),
      sourceEventId: requiredString('sourceEventId', 'SourceEventId'),
      sourceEventTitle: requiredString('sourceEventTitle', 'SourceEventTitle'),
      targetVolunteerId: requiredString(
        'targetVolunteerId',
        'TargetVolunteerId',
      ),
      targetVolunteerName: requiredString(
        'targetVolunteerName',
        'TargetVolunteerName',
      ),
      targetVolunteerEmail: requiredString(
        'targetVolunteerEmail',
        'TargetVolunteerEmail',
      ),
      targetShiftId: requiredString('targetShiftId', 'TargetShiftId'),
      targetShiftTitle: requiredString('targetShiftTitle', 'TargetShiftTitle'),
      targetEventId: requiredString('targetEventId', 'TargetEventId'),
      targetEventTitle: requiredString('targetEventTitle', 'TargetEventTitle'),
      reason: json['reason'] as String? ?? json['Reason'] as String?,
      status: requiredString('status', 'Status'),
      createdAt: requiredDateTime('createdAt', 'CreatedAt'),
      updatedAt: requiredDateTime('updatedAt', 'UpdatedAt'),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'requesterAssignmentId': requesterAssignmentId,
    'requesterVolunteerId': requesterVolunteerId,
    'requesterName': requesterName,
    'requesterEmail': requesterEmail,
    'sourceShiftId': sourceShiftId,
    'sourceShiftTitle': sourceShiftTitle,
    'sourceEventId': sourceEventId,
    'sourceEventTitle': sourceEventTitle,
    'targetVolunteerId': targetVolunteerId,
    'targetVolunteerName': targetVolunteerName,
    'targetVolunteerEmail': targetVolunteerEmail,
    'targetShiftId': targetShiftId,
    'targetShiftTitle': targetShiftTitle,
    'targetEventId': targetEventId,
    'targetEventTitle': targetEventTitle,
    'reason': reason,
    'status': status,
    'createdAt': createdAt.toIso8601String(),
    'updatedAt': updatedAt.toIso8601String(),
  };
}
