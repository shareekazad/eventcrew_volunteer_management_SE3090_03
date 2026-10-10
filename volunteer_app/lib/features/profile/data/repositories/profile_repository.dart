import '../../../../core/api/api_client.dart';
import '../models/skill_model.dart';
import '../models/volunteer_profile_model.dart';

/// Repository for the volunteer's own profile.
///
/// Talks to:
/// - GET  /api/skills              — fetch the skill catalog
/// - GET  /api/volunteers/me       — fetch the current volunteer's profile
/// - POST /api/volunteers/profile  — create or update the profile
class ProfileRepository {
  ProfileRepository({ApiClient? client}) : _client = client ?? ApiClient();

  final ApiClient _client;

  /// GET /api/skills — full skill catalog.
  Future<List<SkillModel>> getSkillCatalog() async {
    final raw = await _client.get('/api/skills');

    if (raw is! List) {
      throw ApiException(
        'Unexpected response shape: expected a list of skills.',
      );
    }

    return raw
        .map((json) => SkillModel.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// GET /api/volunteers/me.
  /// Returns null when the profile does not exist yet (404).
  Future<VolunteerProfileModel?> getMyProfile() async {
    try {
      final raw = await _client.get('/api/volunteers/me');
      if (raw == null) return null;
      if (raw is! Map<String, dynamic>) {
        throw ApiException(
          'Unexpected response shape: expected a profile object.',
        );
      }
      return VolunteerProfileModel.fromJson(raw);
    } on ApiException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }

  /// POST /api/volunteers/profile — create or update profile.
  Future<VolunteerProfileModel> upsertProfile({
    required String emergencyContact,
    String? bio,
    required int maxHoursPerWeek,
    List<String> skillIds = const [],
  }) async {
    final body = <String, dynamic>{
      'emergencyContact': emergencyContact,
      if (bio != null && bio.isNotEmpty) 'bio': bio,
      'maxHoursPerWeek': maxHoursPerWeek,
      'skillIds': skillIds,
    };

    final raw = await _client.post('/api/volunteers/profile', body: body);

    if (raw is! Map<String, dynamic>) {
      throw ApiException(
        'Unexpected response shape: expected a profile object.',
      );
    }

    return VolunteerProfileModel.fromJson(raw);
  }
}