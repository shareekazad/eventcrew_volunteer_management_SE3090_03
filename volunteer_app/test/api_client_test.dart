import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:volunteer_app/core/api/api_client.dart';

void main() {
  test('sends API requests without authentication headers', () async {
    late http.Request request;
    final client = ApiClient(
      client: MockClient((incomingRequest) async {
        request = incomingRequest;
        return http.Response('[]', 200);
      }),
    );

    await client.get('/api/shifts');

    expect(request.headers.keys.map((key) => key.toLowerCase()),
        isNot(contains('authorization')));
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
