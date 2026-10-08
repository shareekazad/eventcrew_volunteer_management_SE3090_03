import '../../../../core/api/api_client.dart';
import '../models/application_model.dart';

/// Repository for volunteer application operations.
///
/// Wraps [ApiClient] to talk to:
/// - POST /api/applications          — submit a new application
/// - GET  /api/applications/mine     — list the current volunteer's applications
///
/// Screens call this; they never touch ApiClient or raw JSON.
class ApplicationRepository {
  ApplicationRepository({ApiClient? client}) : _client = client ?? ApiClient();

  final ApiClient _client;

  // ---------------------------------------------------------------------
  // MY APPLICATIONS
  // ---------------------------------------------------------------------
  /// GET /api/applications/mine — list the current volunteer's applications.
  Future<List<ApplicationModel>> getMyApplications() async {
    final raw = await _client.get('/api/applications/mine');

    if (raw is! List) {
      throw ApiException(
        'Unexpected response shape: expected a list of applications.',
      );
    }

    return raw
        .map((json) => ApplicationModel.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  // ---------------------------------------------------------------------
  // APPLY TO EVENT
  // ---------------------------------------------------------------------
  /// POST /api/applications — submit an application to an event.
  /// Throws [ApiException] with a 409 status if already applied.
  Future<ApplicationModel> applyToEvent({
    required String eventId,
    String? roleRequirementId,
    String? notes,
  }) async {
    final body = <String, dynamic>{
      'eventId': eventId,
      if (roleRequirementId != null && roleRequirementId.isNotEmpty)
        'roleRequirementId': roleRequirementId,
      if (notes != null && notes.isNotEmpty) 'notes': notes,
    };

    final raw = await _client.post('/api/applications', body: body);

    if (raw is! Map<String, dynamic>) {
      throw ApiException(
        'Unexpected response shape: expected an application object.',
      );
    }

    return ApplicationModel.fromJson(raw);
  }
}