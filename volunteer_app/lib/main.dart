import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/auth/auth_provider.dart';
import 'features/auth/presentation/screens/login_screen.dart';
import 'features/events/presentation/screens/discovery_screen.dart';

void main() {
  runApp(const EventCrewApp());
}

/// Root widget of the EventCrew volunteer mobile app.
///
/// Wraps the tree in [ChangeNotifierProvider<AuthProvider>] so any screen can
/// read the current user, and delegates to [AuthGate] to decide whether to
/// show the login screen or the discovery screen.
class EventCrewApp extends StatelessWidget {
  const EventCrewApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<AuthProvider>(
      create: (_) => AuthProvider(),
      child: MaterialApp(
        title: 'EventCrew',
        debugShowCheckedModeBanner: false,
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
          useMaterial3: true,
        ),
        home: const AuthGate(),
      ),
    );
  }
}

/// Decides what to show based on auth state:
///
/// - On first launch, restores the session from secure storage.
/// - While restoring, shows a splash screen.
/// - If logged in → [DiscoveryScreen]
/// - Otherwise → [LoginScreen]
class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  bool _initialised = false;

  @override
  void initState() {
    super.initState();
    // Kick off session restore as soon as the app starts.
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      await context.read<AuthProvider>().initialise();
      if (mounted) setState(() => _initialised = true);
    });
  }

  @override
  Widget build(BuildContext context) {
    if (!_initialised) {
      return const _SplashScreen();
    }

    final isLoggedIn = context.watch<AuthProvider>().isLoggedIn;

    return isLoggedIn ? const DiscoveryScreen() : const LoginScreen();
  }
}

/// Minimal splash screen shown while the app restores the session.
class _SplashScreen extends StatelessWidget {
  const _SplashScreen();

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.event_available, size: 72, color: Colors.deepPurple),
            SizedBox(height: 16),
            Text(
              'EventCrew',
              style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.bold,
                color: Colors.deepPurple,
              ),
            ),
            SizedBox(height: 24),
            CircularProgressIndicator(),
          ],
        ),
      ),
    );
  }
}