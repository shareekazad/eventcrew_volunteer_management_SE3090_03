import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../models/event_model.dart';
import '../services/api_service.dart';

class EventProvider with ChangeNotifier {
  final ApiService _apiService = ApiService();

  List<EventModel> _events = [];
  bool _isLoading = false;
  String? _errorMessage;

  List<EventModel> get events => _events;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  Future<void> fetchEvents() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.get('/events', requiresAuth: false);

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        _events = data
            .map((item) => EventModel.fromJson(item as Map<String, dynamic>))
            .where((event) =>
                event.status.toLowerCase() == 'published' ||
                event.status.toLowerCase() == 'active' ||
                event.status.isEmpty)
            .toList();
      } else {
        _errorMessage = 'Failed to load events (Status: ${response.statusCode})';
      }
    } catch (e) {
      _errorMessage = e.toString().replaceAll('Exception: ', '');
    }

    _isLoading = false;
    notifyListeners();
  }

  EventModel? getEventById(String eventId) {
    try {
      return _events.firstWhere((e) => e.id.toLowerCase() == eventId.toLowerCase());
    } catch (_) {
      return null;
    }
  }
}
