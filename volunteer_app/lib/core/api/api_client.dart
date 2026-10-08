import 'dart:async';
import 'dart:convert';
import 'package:http/http.dart' as http;

import '../auth/token_storage.dart';
import '../config/api_config.dart';

/// Thrown when an API request fails.
class ApiException implements Exception {
  final int? statusCode;
  final String message;

  ApiException(this.message, {this.statusCode});

  @override
  String toString() => 'ApiException(${statusCode ?? 'network'}): $message';
}

/// Thin HTTP client for talking to the EventCrew ASP.NET Core API.
///
/// Responsibilities:
/// - Attach common headers (JSON)
/// - Attach the JWT Authorization header when a token is stored
/// - Enforce a timeout
/// - Translate HTTP errors into typed exceptions
/// - Decode JSON responses
class ApiClient {
  ApiClient({http.Client? client, TokenStorage? tokenStorage})
      : _client = client ?? http.Client(),
        _tokenStorage = tokenStorage ?? TokenStorage();

  final http.Client _client;
  final TokenStorage _tokenStorage;

  // ---------------------------------------------------------------------
  // GET
  // ---------------------------------------------------------------------
  Future<dynamic> get(String path) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$path');

    try {
      final headers = await _buildHeaders();

      // ignore: avoid_print
      print('=== GET $path ===');
      // ignore: avoid_print
      print('Headers: $headers');

      final response = await _client
          .get(uri, headers: headers)
          .timeout(ApiConfig.requestTimeout);

      return _handleResponse(response);
    } on TimeoutException {
      throw ApiException('Request timed out. Is the API running?');
    } on http.ClientException catch (e) {
      throw ApiException('Network error: ${e.message}');
    }
  }

  // ---------------------------------------------------------------------
  // POST
  // ---------------------------------------------------------------------
  Future<dynamic> post(String path, {Map<String, dynamic>? body}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$path');

    try {
      final headers = await _buildHeaders();

      // ignore: avoid_print
      print('=== POST $path ===');
      // ignore: avoid_print
      print('Headers: $headers');
      // ignore: avoid_print
      print('Body: $body');

      final response = await _client
          .post(
            uri,
            headers: headers,
            body: body == null ? null : jsonEncode(body),
          )
          .timeout(ApiConfig.requestTimeout);

      return _handleResponse(response);
    } on TimeoutException {
      throw ApiException('Request timed out. Is the API running?');
    } on http.ClientException catch (e) {
      throw ApiException('Network error: ${e.message}');
    }
  }

  // ---------------------------------------------------------------------
  // Headers (with token)
  // ---------------------------------------------------------------------
  Future<Map<String, String>> _buildHeaders() async {
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    final token = await _tokenStorage.readToken();

    // ignore: avoid_print
    print(
      '=== _buildHeaders === token is '
      '${token == null ? "NULL" : "len:${token.length}"}',
    );

    if (token != null && token.isNotEmpty) {
      headers['Authorization'] = 'Bearer $token';
    }

    return headers;
  }

  // ---------------------------------------------------------------------
  // Response handling
  // ---------------------------------------------------------------------
  dynamic _handleResponse(http.Response response) {
    final body = response.body;

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (body.isEmpty) return null;
      return jsonDecode(body);
    }

    // Try to extract a server error message
    String message = 'HTTP ${response.statusCode}';
    try {
      final decoded = jsonDecode(body);
      if (decoded is Map && decoded['error'] is String) {
        message = decoded['error'] as String;
      }
    } catch (_) {
      // fall through with the default message
    }

    throw ApiException(message, statusCode: response.statusCode);
  }

  /// Cleanup — call when the app shuts down.
  void dispose() => _client.close();
}