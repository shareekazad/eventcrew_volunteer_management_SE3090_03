import '../../../../core/api/api_client.dart';
import '../models/attendance_record.dart';

class AttendanceRepository {
  AttendanceRepository({ApiClient? client}) : _client = client ?? ApiClient();

  final ApiClient _client;

  Future<AttendanceRecord> checkIn({
    required String volunteerId,
    required String eventId,
    required String shiftId,
    required String token,
  }) async {
    final raw = await _client.post(
      '/api/Attendance/check-in',
      body: {
        'volunteerId': volunteerId,
        'eventId': eventId,
        'shiftId': shiftId,
        'token': token,
      },
    );
    if (raw is! Map<String, dynamic>) {
      throw ApiException('Unexpected response shape: expected an attendance record.');
    }
    return AttendanceRecord.fromJson(raw);
  }

  Future<AttendanceRecord> checkOut({
    required String attendanceId,
    required String volunteerId,
  }) async {
    final raw = await _client.post(
      '/api/Attendance/$attendanceId/check-out',
      body: {'volunteerId': volunteerId},
    );
    if (raw is! Map<String, dynamic>) {
      throw ApiException('Unexpected response shape: expected an attendance record.');
    }
    return AttendanceRecord.fromJson(raw);
  }

  Future<List<AttendanceRecord>> getEventAttendance({
    required String eventId,
    required String shiftId,
  }) async {
    final query = Uri(queryParameters: {
      'eventId': eventId,
      'shiftId': shiftId,
    }).query;
    final raw = await _client.get('/api/Attendance?$query');
    if (raw is! List) {
      throw ApiException('Unexpected response shape: expected attendance records.');
    }
    return raw
        .map((record) => AttendanceRecord.fromJson(record as Map<String, dynamic>))
        .toList();
  }

  Future<List<AttendanceRecord>> getVolunteerHistory(String volunteerId) async {
    final raw = await _client.get('/api/Attendance/volunteers/$volunteerId/history');
    if (raw is! List) {
      throw ApiException('Unexpected response shape: expected attendance history.');
    }
    return raw
        .map((record) => AttendanceRecord.fromJson(record as Map<String, dynamic>))
        .toList();
  }
}
