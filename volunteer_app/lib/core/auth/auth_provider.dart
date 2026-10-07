import 'package:flutter/foundation.dart';

import '../../features/auth/data/models/auth_user.dart';
import '../../features/auth/data/repositories/auth_repository.dart';

class AuthProvider extends ChangeNotifier {
  AuthProvider({AuthRepository? repository})
      : _repository = repository ?? AuthRepository();

  final AuthRepository _repository;

  AuthUser? _user;
  bool _isLoading = false;
  String? _errorMessage;

  AuthUser? get user => _user;
  bool get isLoggedIn => _user != null;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  Future<void> initialise() async {
    _setLoading(true);
    _clearError();
    try {
      final restored = await _repository.restoreSession();
      _user = restored;
    } catch (e) {
      debugPrint('INITIALISE ERROR: $e');
      _user = null;
    } finally {
      _setLoading(false);
    }
  }

  Future<bool> register({
    required String fullName,
    required String email,
    required String password,
    String? phoneNumber,
  }) async {
    _setLoading(true);
    _clearError();
    try {
      final user = await _repository.register(
        fullName: fullName,
        email: email,
        password: password,
        phoneNumber: phoneNumber,
      );
      _user = user;
      return true;
    } catch (e) {
      debugPrint('REGISTER ERROR: $e');
      _errorMessage = _humaniseError(e);
      return false;
    } finally {
      _setLoading(false);
    }
  }

  Future<bool> login({
    required String email,
    required String password,
  }) async {
    _setLoading(true);
    _clearError();
    try {
      final user = await _repository.login(email: email, password: password);
      _user = user;
      return true;
    } catch (e) {
      debugPrint('LOGIN ERROR: $e');
      _errorMessage = _humaniseError(e);
      return false;
    } finally {
      _setLoading(false);
    }
  }

  Future<void> logout() async {
    _setLoading(true);
    try {
      await _repository.logout();
      _user = null;
      _clearError();
    } finally {
      _setLoading(false);
    }
  }

  void _setLoading(bool value) {
    _isLoading = value;
    notifyListeners();
  }

  void _clearError() {
    _errorMessage = null;
  }

  String _humaniseError(Object e) {
    final raw = e.toString();

    if (raw.contains('401')) {
      return 'Invalid email or password.';
    }
    if (raw.contains('already exists')) {
      return 'An account with this email already exists.';
    }
    if (raw.contains('timed out')) {
      return 'The server took too long to respond. Please try again.';
    }
    if (raw.contains('Network error') || raw.contains('Failed to fetch')) {
      return 'Network problem. Please check your connection.';
    }

    return 'Something went wrong. Please try again.';
  }
}