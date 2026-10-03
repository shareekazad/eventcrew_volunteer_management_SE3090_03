import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:volunteer_app/core/api/api_client.dart';

void main() {
  test('sends the session token as a bearer authorization header', () async {
    Map<String, String>? requestHeaders;
    final client = ApiClient(
      client: MockClient((request) async {
        requestHeaders = request.headers;
        return http.Response('[]', 200);
      }),
      authToken: 'session-token',
    );

    await client.get('/api/shifts');

    final authorization = requestHeaders!.entries
        .singleWhere((entry) => entry.key.toLowerCase() == 'authorization')
        .value;
    expect(authorization, 'Bearer session-token');
  });

  test('notifies the auth repository on unauthorized responses', () async {
    var clearedSession = false;
    final client =
        ApiClient(
            client: MockClient((_) async => http.Response('Unauthorized', 401)),
            authToken: 'session-token',
          )
          ..onUnauthorized = () async {
            clearedSession = true;
          };

    await expectLater(
      client.get('/api/auth/me'),
      throwsA(
        isA<ApiException>().having(
          (error) => error.statusCode,
          'statusCode',
          401,
        ),
      ),
    );
    expect(clearedSession, isTrue);
  });

  test('supports PUT and DELETE without duplicating HTTP handling', () async {
    final requests = <http.Request>[];
    final client = ApiClient(
      client: MockClient((request) async {
        requests.add(request);
        return http.Response('', 204);
      }),
    );

    await client.put('/api/shifts/shift-id', body: {'capacity': 4});
    await client.delete('/api/shifts/shift-id');

    expect(requests.map((request) => request.method), ['PUT', 'DELETE']);
    expect(jsonDecode(requests.first.body), {'capacity': 4});
    expect(
      requests.first.headers['content-type'],
      contains('application/json'),
    );
  });

  test('extracts validation errors from ASP.NET ProblemDetails', () async {
    final client = ApiClient(
      client: MockClient(
        (_) async => http.Response(
          jsonEncode({
            'title': 'One or more validation errors occurred.',
            'errors': {
              'TargetVolunteerId': ['The target volunteer ID is invalid.'],
            },
          }),
          400,
        ),
      ),
    );

    await expectLater(
      client.post('/api/shift-swaps', body: {}),
      throwsA(
        isA<ApiException>()
            .having((error) => error.statusCode, 'statusCode', 400)
            .having(
              (error) => error.message,
              'message',
              'The target volunteer ID is invalid.',
            ),
      ),
    );
  });
}
