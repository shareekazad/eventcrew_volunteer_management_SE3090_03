/// Data model representing a Shift from ASP.NET Core ShiftResponse.
class ShiftModel {
  final String id;
  final String eventId;
  final String roleRequirementId;
  final String title;
  final String eventName;
  final String roleRequirementName;
  final DateTime startTime;
  final DateTime endTime;
  final int capacity;
  final int assignedCount;
  final int remainingCapacity;
  final String status;
  final DateTime createdAt;
  final DateTime updatedAt;

  const ShiftModel({
    required this.id,
    required this.eventId,
    required this.roleRequirementId,
    required this.title,
    required this.eventName,
    required this.roleRequirementName,
    required this.startTime,
    required this.endTime,
    required this.capacity,
    required this.assignedCount,
    required this.remainingCapacity,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
  });

  factory ShiftModel.fromJson(Map<String, dynamic> json) {
    String requiredString(String camelCase, String pascalCase) {
      final value = json[camelCase] ?? json[pascalCase];
      if (value is String && value.isNotEmpty) return value;
      throw FormatException(
        'Missing or invalid "$camelCase" in shift response.',
      );
    }

    int requiredInt(String camelCase, String pascalCase) {
      final value = json[camelCase] ?? json[pascalCase];
      if (value is num) return value.toInt();
      throw FormatException(
        'Missing or invalid "$camelCase" in shift response.',
      );
    }

    DateTime requiredDateTime(String camelCase, String pascalCase) {
      final value = json[camelCase] ?? json[pascalCase];
      if (value is String) {
        final dateTime = DateTime.tryParse(value);
        if (dateTime != null) return dateTime;
      }
      throw FormatException(
        'Missing or invalid "$camelCase" in shift response.',
      );
    }

    return ShiftModel(
      id: requiredString('id', 'Id'),
      eventId: requiredString('eventId', 'EventId'),
      roleRequirementId: requiredString(
        'roleRequirementId',
        'RoleRequirementId',
      ),
      title: requiredString('title', 'Title'),
      eventName: requiredString('eventName', 'EventName'),
      roleRequirementName: requiredString(
        'roleRequirementName',
        'RoleRequirementName',
      ),
      startTime: requiredDateTime('startTime', 'StartTime'),
      endTime: requiredDateTime('endTime', 'EndTime'),
      capacity: requiredInt('capacity', 'Capacity'),
      assignedCount: requiredInt('assignedCount', 'AssignedCount'),
      remainingCapacity: requiredInt('remainingCapacity', 'RemainingCapacity'),
      status: requiredString('status', 'Status'),
      createdAt: requiredDateTime('createdAt', 'CreatedAt'),
      updatedAt: requiredDateTime('updatedAt', 'UpdatedAt'),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'eventId': eventId,
    'roleRequirementId': roleRequirementId,
    'title': title,
    'eventName': eventName,
    'roleRequirementName': roleRequirementName,
    'startTime': startTime.toIso8601String(),
    'endTime': endTime.toIso8601String(),
    'capacity': capacity,
    'assignedCount': assignedCount,
    'remainingCapacity': remainingCapacity,
    'status': status,
    'createdAt': createdAt.toIso8601String(),
    'updatedAt': updatedAt.toIso8601String(),
  };
}
