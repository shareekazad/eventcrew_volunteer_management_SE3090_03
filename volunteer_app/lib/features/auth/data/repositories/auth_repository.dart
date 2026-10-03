import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../../../core/api/api_client.dart';
import '../models/auth_user_model.dart';

abstract interface class AuthTokenStore {
  Future<String?> read();
  Future<void> write(String token);
  Future<void> clear();
}

class SecureAuthTokenStore implements AuthTokenStore {
  SecureAuthTokenStore({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  static const _tokenKey = 'eventcrew_access_token';
  final FlutterSecureStorage _storage;

  @override
  Future<String?> read() => _storage.read(key: _tokenKey);

  @override
  Future<void> write(String token) =>
      _storage.write(key: _tokenKey, value: token);

  @override
  Future<void> clear() => _storage.delete(key: _tokenKey);
}

/// Manages the volunteer's authenticated API session.
class AuthRepository extends ChangeNotifier {
  static final AuthRepository _instance = AuthRepository._internal();

  factory AuthRepository() => _instance;

  factory AuthRepository.withDependencies({
    required ApiClient apiClient,
    required AuthTokenStore tokenStore,
  }) {
    return AuthRepository._internal(
      apiClient: apiClient,
      tokenStore: tokenStore,
    );
  }

  AuthRepository._internal({ApiClient? apiClient, AuthTokenStore? tokenStore})
    : _apiClient = apiClient ?? ApiClient(),
      _tokenStore = tokenStore ?? SecureAuthTokenStore() {
    _apiClient.onUnauthorized = _handleUnauthorized;
  }

  ApiClient _apiClient;
  final AuthTokenStore _tokenStore;
  AuthUserModel? _currentUser;
  String? _token;
  bool _isInitialized = false;
  bool _isInitializing = false;
  Future<void>? _initialization;
  String? _initializationError;

  AuthUserModel? get currentUser => _currentUser;
  String? get token => _token;
  bool get isAuthenticated => _token != null && _currentUser != null;
  bool get isInitialized => _isInitialized;
  bool get isInitializing => _isInitializing;
  String? get initializationError => _initializationError;

  void updateApiClient(ApiClient client) {
    _apiClient = client;
    _apiClient.onUnauthorized = _handleUnauthorized;
    _apiClient.authToken = _token;
  }

  /// Restores a saved session and verifies it with GET /api/auth/me.
  Future<void> initialize() {
    return _initialization ??= _initialize().whenComplete(() {
      _initialization = null;
    });
  }

  Future<void> _initialize() async {
    _isInitializing = true;
    _initializationError = null;
    notifyListeners();

    try {
      _token = await _tokenStore.read();
      _apiClient.authToken = _token;
      if (_token != null && _token!.isNotEmpty) {
        await getMe();
      } else {
        _token = null;
        _currentUser = null;
      }
    } catch (error) {
      if (error is! ApiException || error.statusCode != 401) {
        _initializationError = error.toString();
      }
    } finally {
      _isInitialized = true;
      _isInitializing = false;
      notifyListeners();
    }
  }

  /// Logs in, persists the access token, then reloads the user from `/me`.
  Future<AuthUserModel> login(String email, String password) async {
    final response = await _apiClient.post(
      '/api/auth/login',
      body: {'email': email.trim(), 'password': password},
    );
    if (response is! Map<String, dynamic>) {
      throw const ApiException('Unexpected response while signing in.');
    }

    final loginResponse = LoginResponseModel.fromJson(response);
    if (loginResponse.user.role != 'Volunteer') {
      throw const ApiException(
        'This app is for volunteer accounts. Sign in with a volunteer account.',
        statusCode: 403,
      );
    }

    _token = loginResponse.accessToken;
    _apiClient.authToken = _token;
    try {
      await _tokenStore.write(_token!);
      final user = await getMe();
      if (user == null) {
        throw const ApiException('The authenticated user could not be loaded.');
      }
      _isInitialized = true;
      _initializationError = null;
      return user;
    } catch (_) {
      await _clearSession();
      rethrow;
    }
  }

  /// Loads the authenticated user from GET /api/auth/me.
  Future<AuthUserModel?> getMe() async {
    if (_token == null) return null;

    final response = await _apiClient.get('/api/auth/me');
    if (response is! Map<String, dynamic>) {
      throw const ApiException(
        'Unexpected response while loading your profile.',
      );
    }
    final user = AuthUserModel.fromJson(response);
    if (user.role != 'Volunteer') {
      await logout();
      throw const ApiException(
        'This app is for volunteer accounts. Sign in with a volunteer account.',
        statusCode: 403,
      );
    }
    _currentUser = user;
    notifyListeners();
    return user;
  }

  Future<void> logout() async {
    await _clearSession();
    _isInitialized = true;
    _initializationError = null;
    notifyListeners();
  }

  Future<void> _handleUnauthorized() => logout();

  Future<void> _clearSession() async {
    _token = null;
    _currentUser = null;
    _apiClient.authToken = null;
    await _tokenStore.clear();
    notifyListeners();
  }
}
