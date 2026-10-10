import '../../../../core/api/api_client.dart';
import '../models/shift_model.dart';

/// Repository for shift-related API calls.
class ShiftRepository {
  ShiftRepository({ApiClient? client}) : _client = client ?? ApiClient();

  final ApiClient _client;

  /// GET /api/Shifts/mine — shifts assigned to the current volunteer.
  Future<List<ShiftModel>> getMyShifts() async {
    final raw = await _client.get('/api/Shifts/mine');

    if (raw is! List) {
      throw ApiException(
        'Unexpected response shape: expected a list of shifts.',
      );
    }

    return raw
        .map((json) => ShiftModel.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// GET /api/Shifts/by-event/{eventId} — all shifts for an event.
  Future<List<ShiftModel>> getShiftsForEvent(String eventId) async {
    final raw = await _client.get('/api/Shifts/by-event/$eventId');

    if (raw is! List) {
      throw ApiException(
        'Unexpected response shape: expected a list of shifts.',
      );
    }

    return raw
        .map((json) => ShiftModel.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// POST /api/Shifts/assignments/{id}/confirm — volunteer confirms assignment.
  Future<ShiftAssignmentModel> confirmAssignment(String assignmentId) async {
    final raw = await _client.post(
      '/api/Shifts/assignments/$assignmentId/confirm',
      body: const {},
    );

    if (raw is! Map<String, dynamic>) {
      throw ApiException('Unexpected response shape.');
    }

    return ShiftAssignmentModel.fromJson(raw);
  }
}