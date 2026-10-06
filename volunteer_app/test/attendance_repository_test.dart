import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:volunteer_app/core/api/api_client.dart';
import 'package:volunteer_app/features/attendance/data/repositories/attendance_repository.dart';

void main() {
  group('AttendanceRepository', () {
    test('submits the scanned token and event/shift/volunteer context', () async {
      late http.Request receivedRequest;
      final client = MockClient((request) async {
        receivedRequest = request;
        return http.Response(
          jsonEncode(_attendanceJson(status: 'CheckedIn')),
          200,
          headers: {'content-type': 'application/json'},
        );
      });
      final repository = AttendanceRepository(client: ApiClient(client: client));

      final result = await repository.checkIn(
        volunteerId: _volunteerId,
        eventId: _eventId,
        shiftId: _shiftId,
        token: _token,
      );

      expect(receivedRequest.method, 'POST');
      expect(receivedRequest.url.path, '/api/Attendance/check-in');
      expect(jsonDecode(receivedRequest.body), {
        'volunteerId': _volunteerId,
        'eventId': _eventId,
        'shiftId': _shiftId,
        'token': _token,
      });
      expect(result.status, 'CheckedIn');
      expect(result.checkInTime, isNotNull);
    });

    test('loads the volunteer attendance state for the selected event and shift',
        () async {
      late Uri receivedUri;
      final client = MockClient((request) async {
        receivedUri = request.url;
        return http.Response(
          jsonEncode([_attendanceJson(status: 'CheckedIn')]),
          200,
          headers: {'content-type': 'application/json'},
        );
      });
      final repository = AttendanceRepository(client: ApiClient(client: client));

      final records = await repository.getEventAttendance(
        eventId: _eventId,
        shiftId: _shiftId,
      );

      expect(receivedUri.path, '/api/Attendance');
      expect(receivedUri.queryParameters, {
        'eventId': _eventId,
        'shiftId': _shiftId,
      });
      expect(records, hasLength(1));
      expect(records.single.volunteerId, _volunteerId);
    });

    test('checks out the specified attendance for its volunteer', () async {
      late http.Request receivedRequest;
      final client = MockClient((request) async {
        receivedRequest = request;
        return http.Response(
          jsonEncode(_attendanceJson(status: 'CheckedOut')),
          200,
          headers: {'content-type': 'application/json'},
        );
      });
      final repository = AttendanceRepository(client: ApiClient(client: client));

      final result = await repository.checkOut(
        attendanceId: _attendanceId,
        volunteerId: _volunteerId,
      );

      expect(receivedRequest.url.path, '/api/Attendance/$_attendanceId/check-out');
      expect(jsonDecode(receivedRequest.body), {'volunteerId': _volunteerId});
      expect(result.status, 'CheckedOut');
      expect(result.verifiedHours, 1.5);
    });

    test('preserves API errors for invalid or expired QR codes', () async {
      final client = MockClient((_) async => http.Response(
            jsonEncode({'error': 'QR token has expired.'}),
            400,
            headers: {'content-type': 'application/json'},
          ));
      final repository = AttendanceRepository(client: ApiClient(client: client));

      await expectLater(
        repository.checkIn(
          volunteerId: _volunteerId,
          eventId: _eventId,
          shiftId: _shiftId,
          token: _token,
        ),
        throwsA(
          isA<ApiException>()
              .having((error) => error.statusCode, 'statusCode', 400)
              .having((error) => error.message, 'message', contains('expired')),
        ),
      );
    });
  });
}

const _volunteerId = '11111111-1111-1111-1111-111111111111';
const _eventId = '22222222-2222-2222-2222-222222222222';
const _shiftId = '33333333-3333-3333-3333-333333333333';
const _attendanceId = '44444444-4444-4444-4444-444444444444';
const _token = 'abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG';

Map<String, dynamic> _attendanceJson({required String status}) => {
      'id': _attendanceId,
      'volunteerId': _volunteerId,
      'eventId': _eventId,
      'eventTitle': 'Community Day',
      'shiftId': _shiftId,
      'shiftTitle': 'Morning support',
      'checkInTime': '2026-10-05T10:00:00+00:00',
      'checkOutTime':
          status == 'CheckedOut' ? '2026-10-05T11:30:00+00:00' : null,
      'status': status,
      'verifiedHours': status == 'CheckedOut' ? 1.5 : 0,
      'createdAt': '2026-10-05T10:00:00+00:00',
      'updatedAt': '2026-10-05T11:30:00+00:00',
    };
