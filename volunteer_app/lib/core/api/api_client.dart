import 'dart:async';
import 'dart:convert';
import 'package:http/http.dart' as http;

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
/// - Enforce a timeout
/// - Translate HTTP errors into typed exceptions
/// - Decode JSON responses
///
/// The client is intentionally stateless — no auth token yet.
/// When JWT auth lands, this is where the Authorization header will be added.
class ApiClient {
  ApiClient({http.Client? client}) : _client = client ?? http.Client();

  final http.Client _client;

  /// GET request. Returns a decoded JSON map or list.
  Future<dynamic> get(String path) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$path');

    try {
      final response = await _client
          .get(uri, headers: _headers)
          .timeout(ApiConfig.requestTimeout);

      return _handleResponse(response);
    } on TimeoutException {
      throw ApiException('Request timed out. Is the API running?');
    } on http.ClientException catch (e) {
      throw ApiException('Network error: ${e.message}');
    }
  }

  /// POST request with an optional JSON body.
  Future<dynamic> post(String path, {Map<String, dynamic>? body}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}$path');

    try {
      final response = await _client
          .post(
            uri,
            headers: _headers,
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

  /// Common headers for every request.
  static const Map<String, String> _headers = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  };

  /// Validates the response and returns decoded JSON.
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