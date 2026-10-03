import 'dart:async';

import 'package:flutter/material.dart';

import 'core/api/api_client.dart';
import 'features/auth/data/repositories/auth_repository.dart';
import 'features/auth/presentation/screens/login_screen.dart';
import 'features/profile/presentation/screens/profile_screen.dart';
import 'features/shift_swaps/presentation/screens/shift_swaps_screen.dart';
import 'features/shifts/presentation/screens/available_shifts_screen.dart';
import 'features/shifts/presentation/screens/my_shifts_screen.dart';

void main() {
  runApp(const EventCrewApp());
}

/// Root widget of the EventCrew volunteer mobile app.
class EventCrewApp extends StatelessWidget {
  const EventCrewApp({super.key, this.authRepository, this.apiClient});

  final AuthRepository? authRepository;
  final ApiClient? apiClient;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'EventCrew',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
        useMaterial3: true,
      ),
      home: MainNavigationScreen(
        authRepository: authRepository,
        apiClient: apiClient,
      ),
    );
  }
}

/// Main navigation shell for Shifts, My Shifts, Shift Swaps, and Profile.
class MainNavigationScreen extends StatefulWidget {
  const MainNavigationScreen({
    super.key,
    this.initialIndex = 0,
    this.authRepository,
    this.apiClient,
  });

  final int initialIndex;
  final AuthRepository? authRepository;
  final ApiClient? apiClient;

  @override
  State<MainNavigationScreen> createState() => _MainNavigationScreenState();
}

class _MainNavigationScreenState extends State<MainNavigationScreen> {
  late int _currentIndex;
  late final AuthRepository _authRepository;
  late final ApiClient _apiClient;

  @override
  void initState() {
    super.initState();
    _currentIndex = widget.initialIndex;
    _apiClient = widget.apiClient ?? ApiClient();
    _authRepository =
        widget.authRepository ??
        (widget.apiClient == null
            ? AuthRepository()
            : AuthRepository.withDependencies(
                apiClient: _apiClient,
                tokenStore: SecureAuthTokenStore(),
              ));
    _authRepository.addListener(_onAuthChanged);
    unawaited(_authRepository.initialize());
  }

  @override
  void dispose() {
    _authRepository.removeListener(_onAuthChanged);
    super.dispose();
  }

  void _onAuthChanged() {
    if (!mounted) return;
    setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final isAuthenticated = _authRepository.isAuthenticated;
    if (!_authRepository.isInitialized || _authRepository.isInitializing) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    if (!isAuthenticated &&
        (_authRepository.token != null ||
            _authRepository.initializationError != null)) {
      return _SessionRestoreError(
        message:
            _authRepository.initializationError ??
            'Your saved session could not be verified.',
        onRetry: _authRepository.initialize,
        onSignOut: _authRepository.logout,
      );
    }

    if (!isAuthenticated) {
      return LoginScreen(authRepository: _authRepository);
    }

    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: [
          AvailableShiftsScreen(apiClient: _apiClient),
          MyShiftsScreen(apiClient: _apiClient),
          ShiftSwapsScreen(apiClient: _apiClient),
          ProfileScreen(authRepository: _authRepository),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (index) {
          setState(() {
            _currentIndex = index;
          });
        },
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.work_outline),
            selectedIcon: Icon(Icons.work),
            label: 'Shifts',
          ),
          NavigationDestination(
            icon: Icon(Icons.schedule_outlined),
            selectedIcon: Icon(Icons.schedule),
            label: 'My Shifts',
          ),
          NavigationDestination(
            icon: Icon(Icons.swap_horiz_outlined),
            selectedIcon: Icon(Icons.swap_horiz),
            label: 'Shift Swaps',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person),
            label: 'Profile',
          ),
        ],
      ),
    );
  }
}

class _SessionRestoreError extends StatelessWidget {
  const _SessionRestoreError({
    required this.message,
    required this.onRetry,
    required this.onSignOut,
  });

  final String message;
  final Future<void> Function() onRetry;
  final Future<void> Function() onSignOut;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Restore session')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.cloud_off_outlined, size: 56),
              const SizedBox(height: 16),
              const Text(
                'Could not verify your saved session',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(message, textAlign: TextAlign.center),
              const SizedBox(height: 20),
              FilledButton.icon(
                onPressed: () => onRetry(),
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
              TextButton(
                onPressed: () async {
                  try {
                    await onSignOut();
                  } catch (error) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(
                            'Could not clear saved session: $error',
                          ),
                        ),
                      );
                    }
                  }
                },
                child: const Text('Sign out'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
