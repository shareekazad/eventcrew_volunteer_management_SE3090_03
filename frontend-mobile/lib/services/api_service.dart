import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

/// ApiService handles cross-platform networking, dynamic host routing,
/// and secure JWT injection via encrypted storage (SE3090 Section 8 & 10 Compliance).
class ApiService {
  static final ApiService _instance = ApiService._internal();
  factory ApiService() => _instance;
  ApiService._internal();

  final FlutterSecureStorage _secureStorage = const FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
    iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock),
  );

  /// Key constants for secure storage
  static const String keyToken = 'jwt_token';
  static const String keyUserId = 'user_id';
  static const String keyFullName = 'full_name';
  static const String keyEmail = 'user_email';
  static const String keyRole = 'user_role';

  /// Section 10: Dynamic API Base URL resolution depending on runtime platform
  String get baseUrl {
    if (kIsWeb) {
      return 'http://localhost:5100/api';
    }
    if (Platform.isAndroid) {
      // Android emulator maps 10.0.2.2 to the host loopback 127.0.0.1
      return 'http://10.0.2.2:5100/api';
    } else if (Platform.isIOS || Platform.isMacOS) {
      // iOS Simulator and macOS desktop connect directly to host localhost:5100
      return 'http://localhost:5100/api';
    }
    return 'http://localhost:5100/api';
  }

  FlutterSecureStorage get secureStorage => _secureStorage;

  /// Retrieves the persisted JWT token from encrypted storage
  Future<String?> getToken() async {
    try {
      return await _secureStorage.read(key: keyToken);
    } catch (e) {
      debugPrint('Error reading secure token: $e');
      return null;
    }
  }

  /// Saves authenticated user session into secure storage
  Future<void> saveSession({
    required String token,
    required String userId,
    required String fullName,
    required String email,
    required String role,
  }) async {
    await _secureStorage.write(key: keyToken, value: token);
    await _secureStorage.write(key: keyUserId, value: userId);
    await _secureStorage.write(key: keyFullName, value: fullName);
    await _secureStorage.write(key: keyEmail, value: email);
    await _secureStorage.write(key: keyRole, value: role);
  }

  /// Clears secure storage on logout
  Future<void> clearSession() async {
    await _secureStorage.deleteAll();
  }

  /// Builds request headers and injects JWT Bearer token if present
  Future<Map<String, String>> _buildHeaders({bool requiresAuth = true}) async {
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (requiresAuth) {
      final token = await getToken();
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
    }

    return headers;
  }

  /// Helper for executing GET requests
  Future<http.Response> get(String endpoint, {bool requiresAuth = true}) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final headers = await _buildHeaders(requiresAuth: requiresAuth);

    try {
      final response = await http
          .get(url, headers: headers)
          .timeout(const Duration(seconds: 10));
      return _validateResponse(response);
    } on SocketException catch (e) {
      throw Exception('Network connection error. Is the ASP.NET Core API running on :5100? ($e)');
    }
  }

  /// Helper for executing POST requests
  Future<http.Response> post(
    String endpoint, {
    Map<String, dynamic>? body,
    bool requiresAuth = true,
  }) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final headers = await _buildHeaders(requiresAuth: requiresAuth);

    try {
      final response = await http
          .post(
            url,
            headers: headers,
            body: body != null ? jsonEncode(body) : null,
          )
          .timeout(const Duration(seconds: 10));
      return _validateResponse(response);
    } on SocketException catch (e) {
      throw Exception('Network connection error. Is the ASP.NET Core API running on :5100? ($e)');
    }
  }

  /// Helper for executing PUT requests
  Future<http.Response> put(
    String endpoint, {
    Map<String, dynamic>? body,
    bool requiresAuth = true,
  }) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final headers = await _buildHeaders(requiresAuth: requiresAuth);

    try {
      final response = await http
          .put(
            url,
            headers: headers,
            body: body != null ? jsonEncode(body) : null,
          )
          .timeout(const Duration(seconds: 10));
      return _validateResponse(response);
    } on SocketException catch (e) {
      throw Exception('Network connection error: $e');
    }
  }

  /// Helper for executing DELETE requests
  Future<http.Response> delete(String endpoint, {bool requiresAuth = true}) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final headers = await _buildHeaders(requiresAuth: requiresAuth);

    try {
      final response = await http
          .delete(url, headers: headers)
          .timeout(const Duration(seconds: 10));
      return _validateResponse(response);
    } on SocketException catch (e) {
      throw Exception('Network connection error: $e');
    }
  }

  /// Response validation with error extraction
  http.Response _validateResponse(http.Response response) {
    if (response.statusCode == 401) {
      // 401 Unauthorized handling
      debugPrint('⚠️ [API Service] 401 Unauthorized encountered at ${response.request?.url}');
    }
    return response;
  }
}
