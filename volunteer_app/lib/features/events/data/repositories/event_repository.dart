import '../../../../core/api/api_client.dart';
import '../models/event_model.dart';

/// Repository for event-related API calls.
///
/// Wraps the ApiClient and converts raw JSON into typed EventModel objects.
/// Screens call this — they never touch ApiClient directly.
class EventRepository {
  EventRepository({ApiClient? client}) : _client = client ?? ApiClient();

  final ApiClient _client;

  /// GET /api/Events — returns all events.
  Future<List<EventModel>> getAllEvents() async {
    final raw = await _client.get('/api/Events');

    if (raw is! List) {
      throw ApiException('Unexpected response shape: expected a list of events.');
    }

    return raw
        .map((json) => EventModel.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// GET /api/Events/{id} — returns a single event, or null if not found.
  Future<EventModel?> getEventById(String id) async {
    try {
      final raw = await _client.get('/api/Events/$id');

      if (raw is! Map<String, dynamic>) {
        throw ApiException('Unexpected response shape: expected an event object.');
      }

      return EventModel.fromJson(raw);
    } on ApiException catch (e) {
      // 404 = not found → return null so callers can show a clean "not found" UI.
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }
}