import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import '../../theme/app_theme.dart';
import 'applications/my_applications_screen.dart';
import 'auth/login_screen.dart';
import 'events/events_screen.dart';
import 'profile/profile_screen.dart';

class HomeScreen extends StatefulWidget {
  static const routeName = '/home';
  final int initialTabIndex;

  const HomeScreen({super.key, this.initialTabIndex = 0});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late int _currentIndex;

  @override
  void initState() {
    super.initState();
    _currentIndex = widget.initialTabIndex;
  }

  void _onTabSelected(int index) {
    setState(() {
      _currentIndex = index;
    });
  }

  Future<void> _handleLogout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirm Logout'),
        content: const Text('Are you sure you want to end your volunteer session?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.statusRejected),
            child: const Text('Logout'),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      final navigator = Navigator.of(context);
      await Provider.of<AuthProvider>(context, listen: false).logout();
      if (!mounted) return;
      navigator.pushNamedAndRemoveUntil(LoginScreen.routeName, (route) => false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = Provider.of<AuthProvider>(context);
    final user = auth.currentUser;

    final titles = [
      'Explore Events',
      'My Applications',
      'Volunteer Profile',
    ];

    final screens = [
      const EventsScreen(),
      MyApplicationsScreen(onBrowseEvents: () => _onTabSelected(0)),
      const ProfileScreen(),
    ];

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              titles[_currentIndex],
              style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 19),
            ),
            if (user != null)
              Text(
                'Signed in as ${user.fullName} (${user.role})',
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                  color: AppTheme.textSecondary,
                ),
              ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Logout',
            icon: const Icon(Icons.logout_rounded, color: AppTheme.statusRejected),
            onPressed: _handleLogout,
          ),
        ],
      ),
      body: IndexedStack(
        index: _currentIndex,
        children: screens,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: _onTabSelected,
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.festival_outlined),
            selectedIcon: Icon(Icons.festival_rounded),
            label: 'Explore Events',
          ),
          NavigationDestination(
            icon: Icon(Icons.assignment_outlined),
            selectedIcon: Icon(Icons.assignment_rounded),
            label: 'My Applications',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person_rounded),
            label: 'My Profile',
          ),
        ],
      ),
    );
  }
}
