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

class EventCrewApp extends StatelessWidget {
  const EventCrewApp({super.key, this.apiClient});

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
      home: MainNavigationScreen(apiClient: apiClient),
    );
  }
}

class MainNavigationScreen extends StatefulWidget {
  const MainNavigationScreen({super.key, this.initialIndex = 0, this.apiClient});

  final int initialIndex;
  final ApiClient? apiClient;

  @override
  State<MainNavigationScreen> createState() => _MainNavigationScreenState();
}

class _MainNavigationScreenState extends State<MainNavigationScreen> {
  late int _currentIndex = widget.initialIndex;
  late final ApiClient _apiClient = widget.apiClient ?? ApiClient();
  late final AuthRepository _authRepository = AuthRepository();
  bool _showRoleSelection = false;
  bool _showLogin = false;

  @override
  void initState() {
    super.initState();
    _authRepository.updateApiClient(_apiClient);
    unawaited(_authRepository.initialize());
  }

  @override
  Widget build(BuildContext context) {
    if (_showLogin) {
      return LoginScreen(
        authRepository: _authRepository,
        onLoginSuccess: () => setState(() {
          _showLogin = false;
          _showRoleSelection = false;
        }),
      );
    }

    if (_showRoleSelection) {
      return Scaffold(
        appBar: AppBar(title: const Text('EventCrew Demo')),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text('Continue as', style: TextStyle(fontSize: 22)),
                const SizedBox(height: 20),
                SizedBox(
                  width: 280,
                  child: FilledButton(
                    onPressed: () => setState(() => _showRoleSelection = false),
                    child: const Text('Volunteer'),
                  ),
                ),
                const SizedBox(height: 8),
                SizedBox(
                  width: 280,
                  child: OutlinedButton(
                    onPressed: () => setState(() => _showLogin = true),
                    child: const Text('Sign in with a volunteer account'),
                  ),
                ),
                const SizedBox(height: 8),
                const Text('Organizer features are available in the EventCrew web app.'),
              ],
            ),
          ),
        ),
      );
    }

    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: [
          AvailableShiftsScreen(apiClient: _apiClient),
          MyShiftsScreen(apiClient: _apiClient),
          ShiftSwapsScreen(apiClient: _apiClient),
          ProfileScreen(onSwitchRole: _switchRole),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (index) => setState(() => _currentIndex = index),
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

  Future<void> _switchRole() async {
    if (_authRepository.isAuthenticated) {
      await _authRepository.logout();
    }
    if (mounted) setState(() => _showRoleSelection = true);
  }
}
