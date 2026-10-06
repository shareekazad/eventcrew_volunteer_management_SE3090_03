class AttendanceRecord {
  final String? id;
  final String volunteerId;
  final String eventId;
  final String eventTitle;
  final String shiftId;
  final String shiftTitle;
  final DateTime? checkInTime;
  final DateTime? checkOutTime;
  final String status;
  final double verifiedHours;

  const AttendanceRecord({
    required this.id,
    required this.volunteerId,
    required this.eventId,
    required this.eventTitle,
    required this.shiftId,
    required this.shiftTitle,
    required this.checkInTime,
    required this.checkOutTime,
    required this.status,
    required this.verifiedHours,
  });

  factory AttendanceRecord.fromJson(Map<String, dynamic> json) {
    return AttendanceRecord(
      id: json['id'] as String?,
      volunteerId: json['volunteerId'] as String,
      eventId: json['eventId'] as String,
      eventTitle: json['eventTitle'] as String? ?? '',
      shiftId: json['shiftId'] as String,
      shiftTitle: json['shiftTitle'] as String? ?? '',
      checkInTime: _parseDateTime(json['checkInTime']),
      checkOutTime: _parseDateTime(json['checkOutTime']),
      status: json['status'] as String,
      verifiedHours: (json['verifiedHours'] as num?)?.toDouble() ?? 0,
    );
  }

  static DateTime? _parseDateTime(dynamic value) =>
      value is String ? DateTime.tryParse(value) : null;
}
