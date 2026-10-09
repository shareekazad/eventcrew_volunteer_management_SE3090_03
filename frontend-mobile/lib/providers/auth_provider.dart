import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../models/user_model.dart';
import '../services/api_service.dart';

class AuthProvider with ChangeNotifier {
  final ApiService _apiService = ApiService();

  UserModel? _currentUser;
  bool _isLoading = false;
  String? _errorMessage;

  UserModel? get currentUser => _currentUser;
  bool get isAuthenticated => _currentUser != null && _currentUser!.token.isNotEmpty;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Attempts to restore session from secure storage upon app startup
  Future<bool> tryAutoLogin() async {
    _isLoading = true;
    notifyListeners();

    try {
      final token = await _apiService.getToken();
      final userId = await _apiService.secureStorage.read(key: ApiService.keyUserId);
      final fullName = await _apiService.secureStorage.read(key: ApiService.keyFullName);
      final email = await _apiService.secureStorage.read(key: ApiService.keyEmail);
      final role = await _apiService.secureStorage.read(key: ApiService.keyRole);

      if (token != null && token.isNotEmpty && userId != null && role == 'Volunteer') {
        _currentUser = UserModel(
          userId: userId,
          fullName: fullName ?? 'Volunteer',
          email: email ?? '',
          role: role ?? 'Volunteer',
          token: token,
        );
        _isLoading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      debugPrint('Auto-login error: $e');
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  /// Handles user login with role protection (Volunteer-only on mobile)
  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.post(
        '/auth/login',
        body: {
          'email': email.trim(),
          'password': password.trim(),
        },
        requiresAuth: false,
      );

      final data = jsonDecode(response.body);

      if (response.statusCode == 200) {
        final role = data['role']?.toString() ?? '';
        
        // Role Protection (Requirement 4): Volunteers only on mobile
        if (role.toLowerCase() != 'volunteer') {
          _errorMessage = 'Access Restricted: This mobile app is exclusively for Volunteers. Organizers should use the Web Portal.';
          _isLoading = false;
          notifyListeners();
          return false;
        }

        final token = data['token']?.toString() ?? '';
        final userId = data['userId']?.toString() ?? '';
        final fullName = data['fullName']?.toString() ?? 'Volunteer';
        final userEmail = data['email']?.toString() ?? email;

        // Save into encrypted storage (iOS Keychain / Android Keystore)
        await _apiService.saveSession(
          token: token,
          userId: userId,
          fullName: fullName,
          email: userEmail,
          role: role,
        );

        _currentUser = UserModel(
          userId: userId,
          fullName: fullName,
          email: userEmail,
          role: role,
          token: token,
        );

        _isLoading = false;
        notifyListeners();
        return true;
      } else {
        _errorMessage = data['message']?.toString() ?? 'Invalid credentials or login failed.';
      }
    } catch (e) {
      _errorMessage = e.toString().replaceAll('Exception: ', '');
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  /// Handles volunteer registration (Role is fixed to "Volunteer")
  Future<bool> register({
    required String fullName,
    required String email,
    required String password,
    required String phoneNumber,
  }) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiService.post(
        '/auth/register',
        body: {
          'fullName': fullName.trim(),
          'email': email.trim(),
          'password': password.trim(),
          'phoneNumber': phoneNumber.trim(),
          'role': 'Volunteer',
        },
        requiresAuth: false,
      );

      final data = jsonDecode(response.body);

      if (response.statusCode == 200 || response.statusCode == 201) {
        final token = data['token']?.toString() ?? '';
        final userId = data['userId']?.toString() ?? '';
        final name = data['fullName']?.toString() ?? fullName;
        final userEmail = data['email']?.toString() ?? email;
        final role = data['role']?.toString() ?? 'Volunteer';

        await _apiService.saveSession(
          token: token,
          userId: userId,
          fullName: name,
          email: userEmail,
          role: role,
        );

        _currentUser = UserModel(
          userId: userId,
          fullName: name,
          email: userEmail,
          role: role,
          token: token,
        );

        _isLoading = false;
        notifyListeners();
        return true;
      } else {
        _errorMessage = data['message']?.toString() ?? 'Registration failed.';
      }
    } catch (e) {
      _errorMessage = e.toString().replaceAll('Exception: ', '');
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  /// Clears secure session and logs out
  Future<void> logout() async {
    await _apiService.clearSession();
    _currentUser = null;
    _errorMessage = null;
    notifyListeners();
  }
}
