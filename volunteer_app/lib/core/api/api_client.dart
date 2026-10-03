import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';

/// Thrown when an API request fails.
class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});

  final int? statusCode;
  final String message;

  @override
  String toString() => message;
}

/// Centralized HTTP client for the EventCrew ASP.NET Core API.
class ApiClient {
  static final ApiClient _shared = ApiClient._();

  factory ApiClient({http.Client? client, String? authToken}) {
    if (client == null && authToken == null) return _shared;
    return ApiClient._(client: client, authToken: authToken);
  }

  ApiClient._({http.Client? client, this.authToken})
    : _client = client ?? http.Client();

  final http.Client _client;
  String? authToken;
  Future<void> Function()? onUnauthorized;

  Future<dynamic> get(String path) => _send('GET', path);

  Future<dynamic> post(String path, {Object? body}) =>
      _send('POST', path, body: body);

  Future<dynamic> put(String path, {Object? body}) =>
      _send('PUT', path, body: body);

  Future<dynamic> delete(String path) => _send('DELETE', path);

  Future<dynamic> _send(String method, String path, {Object? body}) async {
    final relativePath = path.startsWith('/') ? path.substring(1) : path;
    final uri = Uri.parse(ApiConfig.baseUrl).resolve(relativePath);
    final request = http.Request(method, uri)
      ..headers.addAll(_headers(hasBody: body != null));
    if (body != null) request.body = jsonEncode(body);

    try {
      final streamedResponse = await _client
          .send(request)
          .timeout(ApiConfig.requestTimeout);
      final response = await http.Response.fromStream(streamedResponse)
          .timeout(ApiConfig.requestTimeout);
      return await _handleResponse(response);
    } on TimeoutException {
      throw const ApiException(
        'The request timed out. Check your connection and try again.',
      );
    } on http.ClientException catch (error) {
      throw ApiException('Network error: ${error.message}');
    }
  }

  Map<String, String> _headers({required bool hasBody}) => {
    'Accept': 'application/json',
    if (hasBody) 'Content-Type': 'application/json; charset=utf-8',
    if (authToken != null && authToken!.isNotEmpty)
      'Authorization': 'Bearer ${authToken!}',
  };

  Future<dynamic> _handleResponse(http.Response response) async {
    final body = response.body;
    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (body.isEmpty) return null;
      try {
        return jsonDecode(body);
      } on FormatException {
        throw const ApiException(
          'The server returned an invalid JSON response.',
        );
      }
    }

    final message = _errorMessage(response.statusCode, body);
    if (response.statusCode == 401) {
      await onUnauthorized?.call();
    }
    throw ApiException(message, statusCode: response.statusCode);
  }

  String _errorMessage(int statusCode, String body) {
    String? detail;
    String? title;
    Map? errors;

    if (body.isNotEmpty) {
      try {
        final decoded = jsonDecode(body);
        if (decoded is Map) {
          detail = decoded['detail'] as String?;
          title = decoded['title'] as String?;
          errors = decoded['errors'] as Map?;
        }
      } on FormatException {
        // Some proxy/server errors are plain text rather than ProblemDetails.
      }
    }

    final validationMessages = errors?.values
        .whereType<List>()
        .expand((messages) => messages.whereType<String>())
        .toList();
    if (validationMessages != null && validationMessages.isNotEmpty) {
      return validationMessages.join('\n');
    }
    if (detail != null && detail.isNotEmpty) return detail;
    if (title != null && title.isNotEmpty) return title;

    return switch (statusCode) {
      400 => 'The request was invalid. Check the submitted information.',
      401 => 'Your session has expired. Please sign in again.',
      403 => 'You do not have permission to perform this action.',
      404 => 'The requested item could not be found.',
      409 =>
        'The request conflicts with the current data. Refresh and try again.',
      >= 500 => 'The server encountered an error. Please try again later.',
      _ => 'The request failed (HTTP $statusCode).',
    };
  }

  void dispose() => _client.close();
}
