import '../../../../core/api/api_client.dart';
import '../models/shift_swap_model.dart';

/// Repository for handling shift swap operations with the ASP.NET Core API.
class ShiftSwapRepository {
  ShiftSwapRepository({ApiClient? apiClient})
    : _apiClient = apiClient ?? ApiClient();

  final ApiClient _apiClient;

  /// Fetches shift swaps relevant to the logged-in volunteer from GET /api/shift-swaps.
  Future<List<ShiftSwapModel>> getShiftSwaps() async {
    final response = await _apiClient.get('/api/shift-swaps');
    if (response is! List) {
      throw const ApiException(
        'Unexpected response while loading swap requests.',
      );
    }
    return response
        .map((item) => ShiftSwapModel.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  /// Initiates a shift swap request via POST /api/shift-swaps.
  Future<ShiftSwapModel> createShiftSwap({
    required String requesterAssignmentId,
    required String targetVolunteerId,
    required String targetShiftId,
    String? reason,
  }) async {
    final response = await _apiClient.post(
      '/api/shift-swaps',
      body: {
        'requesterAssignmentId': requesterAssignmentId,
        'targetVolunteerId': targetVolunteerId,
        'targetShiftId': targetShiftId,
        if (reason != null && reason.trim().isNotEmpty) 'reason': reason.trim(),
      },
    );
    if (response is! Map<String, dynamic>) {
      throw const ApiException(
        'Unexpected response while creating swap request.',
      );
    }
    return ShiftSwapModel.fromJson(response);
  }

  /// Accepts a shift swap request directed to the logged-in volunteer (POST /api/shift-swaps/{id}/accept).
  Future<ShiftSwapModel> acceptShiftSwap(String swapId) async {
    final response = await _apiClient.post('/api/shift-swaps/$swapId/accept');
    if (response is! Map<String, dynamic>) {
      throw const ApiException(
        'Unexpected response while accepting swap request.',
      );
    }
    return ShiftSwapModel.fromJson(response);
  }

  /// Declines a shift swap request directed to the logged-in volunteer (POST /api/shift-swaps/{id}/reject).
  Future<ShiftSwapModel> rejectShiftSwap(String swapId) async {
    final response = await _apiClient.post('/api/shift-swaps/$swapId/reject');
    if (response is! Map<String, dynamic>) {
      throw const ApiException(
        'Unexpected response while declining swap request.',
      );
    }
    return ShiftSwapModel.fromJson(response);
  }

  /// Cancels a pending shift swap request created by the logged-in volunteer (POST /api/shift-swaps/{id}/cancel).
  Future<ShiftSwapModel> cancelShiftSwap(String swapId) async {
    final response = await _apiClient.post('/api/shift-swaps/$swapId/cancel');
    if (response is! Map<String, dynamic>) {
      throw const ApiException(
        'Unexpected response while cancelling swap request.',
      );
    }
    return ShiftSwapModel.fromJson(response);
  }
}
