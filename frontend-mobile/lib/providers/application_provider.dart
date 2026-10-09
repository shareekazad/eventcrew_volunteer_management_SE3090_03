import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../models/application_model.dart';
import '../services/api_service.dart';

class ApplicationProvider with ChangeNotifier {
  final ApiService _apiService = ApiService();

  List<ApplicationModel> _applications = [];
  bool _isLoading = false;
  String? _errorMessage;

  List<ApplicationModel> get applications => _applications;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Fetches all applications for the logged-in volunteer
  Future<void> fetchMyApplications() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.get('/applications', requiresAuth: true);

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        _applications = data
            .map((item) => ApplicationModel.fromJson(item as Map<String, dynamic>))
            .toList();
      } else {
        _errorMessage = 'Failed to load applications (${response.statusCode})';
      }
    } catch (e) {
      _errorMessage = e.toString().replaceAll('Exception: ', '');
    }

    _isLoading = false;
    notifyListeners();
  }

  /// Submits an application for an event
  Future<bool> applyForEvent({
    required String eventId,
    String? roleRequirementId,
    String? notes,
  }) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final body = {
        'eventId': eventId,
        'roleRequirementId': roleRequirementId,
        'notes': notes ?? '',
      };

      final response = await _apiService.post(
        '/applications',
        body: body,
        requiresAuth: true,
      );

      final data = jsonDecode(response.body);

      if (response.statusCode == 200 || response.statusCode == 201) {
        // Refresh application list so new entry appears immediately
        await fetchMyApplications();
        _isLoading = false;
        notifyListeners();
        return true;
      } else if (response.statusCode == 409) {
        _errorMessage = 'You have already applied for this event.';
      } else {
        _errorMessage = data['message']?.toString() ?? 'Failed to submit application.';
      }
    } catch (e) {
      _errorMessage = e.toString().replaceAll('Exception: ', '');
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  /// Checks if volunteer has already applied to an event
  bool hasApplied(String eventId) {
    return _applications.any((a) => a.eventId.toLowerCase() == eventId.toLowerCase());
  }
}
