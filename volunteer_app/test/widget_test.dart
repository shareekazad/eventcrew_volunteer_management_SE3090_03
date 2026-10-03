import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:volunteer_app/core/api/api_client.dart';
import 'package:volunteer_app/features/auth/data/repositories/auth_repository.dart';
import 'package:volunteer_app/features/shifts/data/models/shift_model.dart';
import 'package:volunteer_app/features/shifts/presentation/widgets/shift_card.dart';
import 'package:volunteer_app/main.dart';

void main() {
  late AuthRepository authRepository;
  late ApiClient apiClient;

  setUp(() {
    apiClient = ApiClient(
      client: MockClient((request) async {
        if (request.url.path == '/api/auth/login') {
          return http.Response(
            jsonEncode({
              'accessToken': 'test-token',
              'expiresAt': '2026-10-03T10:00:00Z',
              'user': {
                'id': 'user-id',
                'fullName': 'Alex Volunteer',
                'email': 'alex@example.test',
                'role': 'Volunteer',
              },
            }),
            200,
          );
        }
        if (request.url.path == '/api/auth/me') {
          return http.Response(
            jsonEncode({
              'id': 'user-id',
              'fullName': 'Alex Volunteer',
              'email': 'alex@example.test',
              'role': 'Volunteer',
            }),
            200,
          );
        }
        if (request.url.path == '/api/shifts/my-assignments') {
          return http.Response(
            jsonEncode({'volunteerId': 'profile-id', 'assignments': []}),
            200,
          );
        }
        return http.Response('[]', 200);
      }),
    );
    authRepository = AuthRepository.withDependencies(
      apiClient: apiClient,
      tokenStore: _MemoryAuthTokenStore(),
    );
  });

  testWidgets('EventCrewApp displays the real volunteer login screen', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      EventCrewApp(authRepository: authRepository, apiClient: apiClient),
    );
    await tester.pumpAndSettle();

    expect(find.text('Volunteer Login'), findsOneWidget);
    expect(find.text('EventCrew Mobile'), findsOneWidget);
    expect(find.text('Log In'), findsOneWidget);
  });

  testWidgets('login form reports missing credentials', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      EventCrewApp(authRepository: authRepository, apiClient: apiClient),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('Log In'));
    await tester.pumpAndSettle();

    expect(find.text('Please enter your email'), findsOneWidget);
    expect(find.text('Please enter your password'), findsOneWidget);
  });

  testWidgets('successful login opens authenticated shift navigation', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      EventCrewApp(authRepository: authRepository, apiClient: apiClient),
    );
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byType(TextFormField).at(0),
      'alex@example.test',
    );
    await tester.enterText(find.byType(TextFormField).at(1), 'password');
    await tester.tap(find.text('Log In'));
    await tester.pumpAndSettle();

    expect(find.text('Available Shifts'), findsOneWidget);
    expect(find.text('My Shifts'), findsWidgets);
    expect(find.text('Shift Swaps'), findsWidgets);
    expect(find.text('Volunteer Login'), findsNothing);

    await tester.tap(find.text('My Shifts').last);
    await tester.pumpAndSettle();
    expect(find.text('No shifts assigned yet'), findsOneWidget);
  });

  testWidgets('ShiftCard displays shift details and capacity', (
    WidgetTester tester,
  ) async {
    final shift = ShiftModel(
      id: 'shift-1',
      eventId: 'event-1',
      roleRequirementId: 'role-1',
      title: 'Welcome Desk',
      eventName: 'Community Festival',
      roleRequirementName: 'Guest Services',
      startTime: DateTime(2026, 10, 5, 9),
      endTime: DateTime(2026, 10, 5, 12),
      capacity: 8,
      assignedCount: 3,
      remainingCapacity: 5,
      status: 'Scheduled',
      createdAt: DateTime(2026, 9, 1),
      updatedAt: DateTime(2026, 9, 1),
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: ShiftCard(shift: shift)),
      ),
    );

    expect(find.text('Welcome Desk'), findsOneWidget);
    expect(find.text('Community Festival'), findsOneWidget);
    expect(find.text('Role: Guest Services'), findsOneWidget);
    expect(find.text('5 of 8 spots remaining'), findsOneWidget);
  });
}

class _MemoryAuthTokenStore implements AuthTokenStore {
  String? token;

  @override
  Future<void> clear() async {
    token = null;
  }

  @override
  Future<String?> read() async => token;

  @override
  Future<void> write(String value) async {
    token = value;
  }
}
