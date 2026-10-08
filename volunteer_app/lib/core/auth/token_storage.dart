import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Secure local storage for the JWT and cached user info.
///
/// On native platforms (Android, iOS, Windows, macOS, Linux), this uses
/// [FlutterSecureStorage] which delegates to the platform's encrypted store
/// (EncryptedSharedPreferences on Android, Keychain on iOS).
///
/// On web, secure storage requires special headers that Flutter's dev server
/// doesn't set. We fall back to a **static in-memory cache** so the token
/// survives across different `TokenStorage()` instances within the same
/// page session (until a hard refresh or hot restart).
class TokenStorage {
  static const _tokenKey = 'auth_token';
  static const _userIdKey = 'auth_user_id';
  static const _emailKey = 'auth_email';
  static const _fullNameKey = 'auth_full_name';
  static const _roleKey = 'auth_role';
  static const _expiresAtKey = 'auth_expires_at';

  /// Static so all TokenStorage instances share the same cache.
  static final Map<String, String> _webCache = {};

  final FlutterSecureStorage _storage;

  TokenStorage({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  // ---------------------------------------------------------------------
  // Low-level helpers that switch based on platform
  // ---------------------------------------------------------------------
  Future<void> _write(String key, String value) async {
    if (kIsWeb) {
      _webCache[key] = value;
      return;
    }
    await _storage.write(key: key, value: value);
  }

  Future<String?> _read(String key) async {
    if (kIsWeb) {
      return _webCache[key];
    }
    return _storage.read(key: key);
  }

  Future<void> _delete(String key) async {
    if (kIsWeb) {
      _webCache.remove(key);
      return;
    }
    await _storage.delete(key: key);
  }

  // ---------------------------------------------------------------------
  // Save / read / clear
  // ---------------------------------------------------------------------
  Future<void> saveSession({
    required String token,
    required String userId,
    required String email,
    required String fullName,
    required String role,
    required DateTime expiresAt,
  }) async {
    await Future.wait([
      _write(_tokenKey, token),
      _write(_userIdKey, userId),
      _write(_emailKey, email),
      _write(_fullNameKey, fullName),
      _write(_roleKey, role),
      _write(_expiresAtKey, expiresAt.toIso8601String()),
    ]);
  }

  Future<String?> readToken() => _read(_tokenKey);
  Future<String?> readUserId() => _read(_userIdKey);
  Future<String?> readEmail() => _read(_emailKey);
  Future<String?> readFullName() => _read(_fullNameKey);
  Future<String?> readRole() => _read(_roleKey);

  Future<DateTime?> readExpiresAt() async {
    final raw = await _read(_expiresAtKey);
    if (raw == null) return null;
    return DateTime.tryParse(raw);
  }

  Future<bool> hasValidSession() async {
    final token = await readToken();
    if (token == null || token.isEmpty) return false;

    final expiresAt = await readExpiresAt();
    if (expiresAt == null) return false;

    return expiresAt.isAfter(DateTime.now());
  }

  Future<void> clear() async {
    await Future.wait([
      _delete(_tokenKey),
      _delete(_userIdKey),
      _delete(_emailKey),
      _delete(_fullNameKey),
      _delete(_roleKey),
      _delete(_expiresAtKey),
    ]);
  }
}