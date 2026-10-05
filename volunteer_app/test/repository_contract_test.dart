import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:volunteer_app/core/api/api_client.dart';
import 'package:volunteer_app/features/shift_swaps/data/repositories/shift_swap_repository.dart';
import 'package:volunteer_app/features/shifts/data/repositories/shift_repository.dart';

void main() {
  final shiftJson = <String, dynamic>{
    'id': 'shift-id',
    'eventId': 'event-id',
    'roleRequirementId': 'role-id',
    'title': 'Welcome Desk',
    'eventName': 'Community Festival',
    'roleRequirementName': 'Guest Services',
    'startTime': '2026-10-05T09:00:00Z',
    'endTime': '2026-10-05T12:00:00Z',
    'capacity': 8,
    'assignedCount': 3,
    'remainingCapacity': 5,
    'status': 'Scheduled',
    'createdAt': '2026-09-01T00:00:00Z',
    'updatedAt': '2026-09-02T00:00:00Z',
  };

  final swapJson = <String, dynamic>{
    'id': 'swap-id',
    'requesterAssignmentId': 'assignment-id',
    'requesterVolunteerId': 'requester-profile-id',
    'requesterName': 'Alex Volunteer',
    'requesterEmail': 'alex@example.test',
    'sourceShiftId': 'source-shift-id',
    'sourceShiftTitle': 'Morning Shift',
    'sourceEventId': 'source-event-id',
    'sourceEventTitle': 'Source Event',
    'targetVolunteerId': 'target-profile-id',
    'targetVolunteerName': 'Sam Volunteer',
    'targetVolunteerEmail': 'sam@example.test',
    'targetShiftId': 'target-shift-id',
    'targetShiftTitle': 'Afternoon Shift',
    'targetEventId': 'target-event-id',
    'targetEventTitle': 'Target Event',
    'reason': 'Schedule conflict',
    'status': 'Pending_Target',
    'createdAt': '2026-10-01T10:00:00Z',
    'updatedAt': '2026-10-01T10:00:00Z',
  };

  test('shift repository parses the GET /api/shifts DTO', () async {
    late Uri requestUri;
    final api = ApiClient(
      client: MockClient((request) async {
        requestUri = request.url;
        return http.Response(jsonEncode([shiftJson]), 200);
      }),
    );

    final shifts = await ShiftRepository(apiClient: api).getAllShifts();

    expect(requestUri.path, '/api/shifts');
    expect(shifts, hasLength(1));
    expect(shifts.single.roleRequirementName, 'Guest Services');
    expect(shifts.single.remainingCapacity, 5);
  });

  test('assignments response includes volunteer profile and confirmed assignment IDs', () async {
    late Uri requestUri;
    final api = ApiClient(
      client: MockClient((request) async {
        requestUri = request.url;
        return http.Response(
          jsonEncode({
            'volunteerId': 'volunteer-profile-id',
            'assignments': [
              {
                'assignmentId': 'assignment-id',
                'shiftId': 'shift-id',
                'title': 'Welcome Desk',
                'eventName': 'Community Festival',
                'roleRequirementName': 'Guest Services',
                'startTime': '2026-10-05T09:00:00Z',
                'endTime': '2026-10-05T12:00:00Z',
                'status': 'Confirmed',
              },
            ],
          }),
          200,
        );
      }),
    );

    final result = await ShiftRepository(apiClient: api).getMyAssignments();

    expect(requestUri.path, '/api/shifts/my-assignments');
    expect(result.volunteerId, 'volunteer-profile-id');
    expect(result.assignments.single.assignmentId, 'assignment-id');
    expect(result.assignments.single.status, 'Confirmed');
  });

  test(
    'shift swap repository parses requests and sends the API create contract',
    () async {
      late http.Request request;
      final api = ApiClient(
        client: MockClient((incomingRequest) async {
          request = incomingRequest;
          return http.Response(jsonEncode(swapJson), 201);
        }),
      );

      final swaps = ShiftSwapRepository(apiClient: api);
      final created = await swaps.createShiftSwap(
        requesterAssignmentId: 'assignment-id',
        targetVolunteerId: 'target-profile-id',
        targetShiftId: 'target-shift-id',
        reason: 'Schedule conflict',
      );

      expect(request.method, 'POST');
      expect(request.url.path, '/api/shift-swaps');
      expect(jsonDecode(request.body), {
        'requesterAssignmentId': 'assignment-id',
        'targetVolunteerId': 'target-profile-id',
        'targetShiftId': 'target-shift-id',
        'reason': 'Schedule conflict',
      });
      expect(created.status, 'Pending_Target');
      expect(created.requesterVolunteerId, 'requester-profile-id');

      final listApi = ApiClient(
        client: MockClient(
          (_) async => http.Response(jsonEncode([swapJson]), 200),
        ),
      );
      final listed = await ShiftSwapRepository(apiClient: listApi)
          .getShiftSwaps();
      expect(listed.single.sourceShiftTitle, 'Morning Shift');
    },
  );
}
