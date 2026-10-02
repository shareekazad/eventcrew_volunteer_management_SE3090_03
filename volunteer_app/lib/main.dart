import 'package:flutter/material.dart';

import 'features/events/presentation/screens/discovery_screen.dart';

void main() {
  runApp(const EventCrewApp());
}

/// Root widget of the EventCrew volunteer mobile app.
///
/// Wires up theming and sets the DiscoveryScreen as the home screen.
class EventCrewApp extends StatelessWidget {
  const EventCrewApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'EventCrew',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
        useMaterial3: true,
      ),
      home: const DiscoveryScreen(),
    );
  }
}