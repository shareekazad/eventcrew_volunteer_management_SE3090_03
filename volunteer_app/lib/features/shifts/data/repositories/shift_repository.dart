import '../../../../core/api/api_client.dart';
import '../models/shift_assignment_model.dart';
import '../models/shift_model.dart';

/// Repository for fetching shift data from ASP.NET Core API.
class ShiftRepository {
  ShiftRepository({ApiClient? apiClient})
    : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  /// Fetches shifts assigned to the current volunteer from GET /api/shifts/my-shifts.
  Future<List<ShiftModel>> getMyShifts() async {
    final response = await _apiClient.get('/api/shifts/my-shifts');
    if (response is! List) {
      throw const ApiException(
        'Unexpected response while loading your shifts.',
      );
    }
    return response
        .map((item) => ShiftModel.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  /// Fetches the current volunteer profile ID and confirmed assignments.
  Future<VolunteerAssignmentsModel> getMyAssignments() async {
    final response = await _apiClient.get('/api/shifts/my-assignments');
    if (response is! Map<String, dynamic>) {
      throw const ApiException(
        'Unexpected response while loading your assignments.',
      );
    }
    return VolunteerAssignmentsModel.fromJson(response);
  }

  /// Fetches all shifts available from GET /api/shifts.
  Future<List<ShiftModel>> getAllShifts() async {
    final response = await _apiClient.get('/api/shifts');
    if (response is! List) {
      throw const ApiException('Unexpected response while loading shifts.');
    }
    return response
        .map((item) => ShiftModel.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  /// Fetches a specific shift by ID from GET /api/shifts/{id}.
  Future<ShiftModel> getShiftById(String id) async {
    final response = await _apiClient.get('/api/shifts/$id');
    if (response is! Map<String, dynamic>) {
      throw const ApiException(
        'Unexpected response while loading shift details.',
      );
    }
    return ShiftModel.fromJson(response);
  }
}
