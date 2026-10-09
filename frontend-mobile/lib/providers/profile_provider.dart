import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import '../models/skill_model.dart';
import '../models/volunteer_profile_model.dart';
import '../services/api_service.dart';

class ProfileProvider with ChangeNotifier {
  final ApiService _apiService = ApiService();

  VolunteerProfileModel? _profile;
  List<SkillModel> _availableSkills = [];
  File? _avatarFile;
  bool _isLoading = false;
  String? _errorMessage;

  VolunteerProfileModel? get profile => _profile;
  List<SkillModel> get availableSkills => _availableSkills;
  File? get avatarFile => _avatarFile;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  void setAvatarFile(File? file) {
    _avatarFile = file;
    notifyListeners();
  }

  /// Fetches current volunteer profile
  Future<void> fetchProfile() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.get('/volunteers/me', requiresAuth: true);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        _profile = VolunteerProfileModel.fromJson(data);
      } else if (response.statusCode == 404) {
        // Profile not created yet
        _profile = null;
      } else {
        _errorMessage = 'Failed to load profile (${response.statusCode})';
      }
    } catch (e) {
      _errorMessage = e.toString().replaceAll('Exception: ', '');
    }

    _isLoading = false;
    notifyListeners();
  }

  /// Fetches system skills for skill builder FilterChips
  Future<void> fetchSkills() async {
    try {
      final response = await _apiService.get('/skills', requiresAuth: false);

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        _availableSkills = data
            .map((s) => SkillModel.fromJson(s as Map<String, dynamic>))
            .toList();
      }
    } catch (e) {
      debugPrint('Could not fetch skills from /api/skills: $e');
    }

    // If API returned empty list or had error, fallback to platform's standard 5 skills
    if (_availableSkills.isEmpty) {
      _availableSkills = [
        SkillModel(id: 'first-aid', name: 'First Aid', category: 'Medical'),
        SkillModel(id: 'crowd-safety', name: 'Crowd Safety', category: 'Operations'),
        SkillModel(id: 'av-tech', name: 'Audio/Visual', category: 'Technical'),
        SkillModel(id: 'logistics', name: 'Logistics', category: 'Operations'),
        SkillModel(id: 'it-support', name: 'IT Support', category: 'Technical'),
      ];
    }

    notifyListeners();
  }

  /// Creates or updates volunteer profile
  Future<bool> saveProfile({
    required String emergencyContact,
    required String bio,
    required int maxHoursPerWeek,
    required List<String> skillIds,
  }) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      // Map any non-GUID fallback skill IDs if needed
      final validSkillGuids = <String>[];
      final guidRegex = RegExp(r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$');

      for (final id in skillIds) {
        if (guidRegex.hasMatch(id)) {
          validSkillGuids.add(id);
        } else {
          // Look up matching skill by name in _availableSkills
          final matched = _availableSkills.firstWhere(
            (s) => s.id == id || s.name.toLowerCase() == id.toLowerCase(),
            orElse: () => SkillModel(id: '', name: '', category: ''),
          );
          if (guidRegex.hasMatch(matched.id)) {
            validSkillGuids.add(matched.id);
          }
        }
      }

      final body = {
        'emergencyContact': emergencyContact.trim(),
        'bio': bio.trim(),
        'maxHoursPerWeek': maxHoursPerWeek,
        'skillIds': validSkillGuids,
      };

      final response = await _apiService.post(
        '/volunteers/profile',
        body: body,
        requiresAuth: true,
      );

      final data = jsonDecode(response.body);

      if (response.statusCode == 200 || response.statusCode == 201) {
        _profile = VolunteerProfileModel.fromJson(data);
        _isLoading = false;
        notifyListeners();
        return true;
      } else {
        _errorMessage = data['message']?.toString() ?? 'Failed to update profile.';
      }
    } catch (e) {
      _errorMessage = e.toString().replaceAll('Exception: ', '');
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }
}
