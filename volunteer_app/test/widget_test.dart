import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:volunteer_app/core/api/api_client.dart';
import 'package:volunteer_app/features/shifts/data/models/shift_model.dart';
import 'package:volunteer_app/features/shifts/presentation/widgets/shift_card.dart';
import 'package:volunteer_app/main.dart';

void main() {
  testWidgets('EventCrew opens directly in the demo volunteer interface', (
    WidgetTester tester,
  ) async {
    final apiClient = ApiClient(
      client: MockClient((_) async => http.Response('[]', 200)),
    );

    await tester.pumpWidget(EventCrewApp(apiClient: apiClient));
    await tester.pumpAndSettle();

    expect(find.text('Available Shifts'), findsOneWidget);
    expect(find.text('My Shifts'), findsWidgets);
    expect(find.text('Shift Swaps'), findsWidgets);
    expect(find.text('Volunteer Login'), findsNothing);
  });

  testWidgets('profile can switch back to demo role selection', (
    WidgetTester tester,
  ) async {
    final apiClient = ApiClient(
      client: MockClient((_) async => http.Response('[]', 200)),
    );

    await tester.pumpWidget(EventCrewApp(apiClient: apiClient));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Profile'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Switch Role'));
    await tester.pumpAndSettle();

    expect(find.text('Continue as'), findsOneWidget);
    expect(find.text('Volunteer'), findsOneWidget);
    expect(find.text('Organizer features are available in the EventCrew web app.'), findsOneWidget);
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
      MaterialApp(home: Scaffold(body: ShiftCard(shift: shift))),
    );

    expect(find.text('Welcome Desk'), findsOneWidget);
    expect(find.text('Community Festival'), findsOneWidget);
    expect(find.text('Role: Guest Services'), findsOneWidget);
    expect(find.text('5 of 8 spots remaining'), findsOneWidget);
  });
}
