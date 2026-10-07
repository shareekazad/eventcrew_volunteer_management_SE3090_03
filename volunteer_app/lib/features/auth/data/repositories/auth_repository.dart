import '../../../../core/api/api_client.dart';
import '../../../../core/auth/token_storage.dart';
import '../models/auth_user.dart';

/// Repository for authentication flows: register, login, and current-user.
///
/// Talks to the ASP.NET Core `/api/Auth/*` endpoints via [ApiClient].
/// Persists the JWT + user info to [TokenStorage] after a successful login
/// or registration so subsequent requests are authenticated automatically.
class AuthRepository {
  AuthRepository({ApiClient? apiClient, TokenStorage? tokenStorage})
      : _api = apiClient ?? ApiClient(),
        _storage = tokenStorage ?? TokenStorage();

  final ApiClient _api;
  final TokenStorage _storage;

  // ---------------------------------------------------------------------
  // REGISTER
  // ---------------------------------------------------------------------
  /// Registers a new volunteer and returns the authenticated [AuthUser].
  /// Throws [ApiException] on failure (e.g., email already used, validation).
  Future<AuthUser> register({
    required String fullName,
    required String email,
    required String password,
    String? phoneNumber,
  }) async {
    final data = await _api.post('/api/Auth/register', body: {
      'fullName': fullName,
      'email': email,
      'password': password,
      if (phoneNumber != null && phoneNumber.isNotEmpty)
        'phoneNumber': phoneNumber,
    });

    final user = AuthUser.fromJson(data as Map<String, dynamic>);
    await _persistSession(user);
    return user;
  }

  // ---------------------------------------------------------------------
  // LOGIN
  // ---------------------------------------------------------------------
  /// Logs in an existing volunteer and returns the authenticated [AuthUser].
  /// Throws [ApiException] with a 401 status on invalid credentials.
  Future<AuthUser> login({
    required String email,
    required String password,
  }) async {
    final data = await _api.post('/api/Auth/login', body: {
      'email': email,
      'password': password,
    });

    final user = AuthUser.fromJson(data as Map<String, dynamic>);
    await _persistSession(user);
    return user;
  }

  // ---------------------------------------------------------------------
  // ME — restore session from a persisted token
  // ---------------------------------------------------------------------
  /// Fetches the current user from the backend using the stored token.
  /// Returns null if the token is missing, expired, or rejected.
  Future<AuthUser?> restoreSession() async {
    final hasValid = await _storage.hasValidSession();
    if (!hasValid) return null;

    try {
      final data = await _api.get('/api/Auth/me');
      if (data == null) return null;

      final user = AuthUser.fromJson(data as Map<String, dynamic>);
      await _persistSession(user);
      return user;
    } on ApiException catch (e) {
      // 401 → stale token; 4xx → invalid; clear and return null
      if (e.statusCode == 401 ||
          (e.statusCode != null && e.statusCode! < 500)) {
        await _storage.clear();
      }
      return null;
    }
  }

  // ---------------------------------------------------------------------
  // LOGOUT
  // ---------------------------------------------------------------------
  Future<void> logout() async {
    await _storage.clear();
  }

  // ---------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------
  Future<void> _persistSession(AuthUser user) async {
    await _storage.saveSession(
      token: user.token,
      userId: user.userId,
      email: user.email,
      fullName: user.fullName,
      role: user.role,
      expiresAt: user.expiresAt,
    );
  }
}