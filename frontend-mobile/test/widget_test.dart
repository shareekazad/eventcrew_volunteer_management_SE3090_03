import 'package:flutter_test/flutter_test.dart';
import 'package:eventcrew_mobile/main.dart';

void main() {
  testWidgets('EventCrew mobile app smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const EventCrewMobileApp());
    expect(find.byType(EventCrewMobileApp), findsOneWidget);
  });
}
