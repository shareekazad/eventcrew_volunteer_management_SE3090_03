import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../../core/auth/auth_provider.dart';
import '../../../applications/presentation/screens/my_applications_screen.dart';
import '../../../profile/presentation/screens/profile_screen.dart';
import '../../../shifts/presentation/screens/my_shifts_screen.dart';
import '../../data/models/event_model.dart';
import '../../data/repositories/event_repository.dart';
import '../widgets/event_card.dart';
import 'event_detail_screen.dart';
import '../../../attendance/presentation/screens/attendance_screen.dart';

/// Event discovery feed — the volunteer's first screen.
///
/// Features:
/// - Search bar to filter events by title/description/category
/// - Category filter chips (All, and each distinct category in the list)
/// - Pull-to-refresh
/// - Profile menu (My Profile, My Applications, My Shifts, Logout)
class DiscoveryScreen extends StatefulWidget {
  const DiscoveryScreen({super.key});

  @override
  State<DiscoveryScreen> createState() => _DiscoveryScreenState();
}

class _DiscoveryScreenState extends State<DiscoveryScreen> {
  final EventRepository _repository = EventRepository();
  final TextEditingController _searchController = TextEditingController();

  // ---- UI state ----
  bool _isLoading = true;
  String? _errorMessage;
  List<EventModel> _events = [];
  int _selectedDestination = 0;

  /// Selected category filter. 'All' means no filter.
  String _selectedCategory = 'All';

  @override
  void initState() {
    super.initState();
    _loadEvents();
    _searchController.addListener(() => setState(() {}));
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadEvents() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final events = await _repository.getAllEvents();
      setState(() {
        _events = events;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _errorMessage = e.toString();
        _isLoading = false;
      });
    }
  }

  Future<void> _handleLogout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sign out?'),
        content: const Text('You will be returned to the login screen.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const Text('Sign out'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;
    if (!mounted) return;

    await context.read<AuthProvider>().logout();
  }

  void _openMyApplications() {
    Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => const MyApplicationsScreen()),
    );
  }

  void _openMyShifts() {
    Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => const MyShiftsScreen()),
    );
  }

  void _openProfile() {
    Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => const ProfileScreen()),
    );
  }

  // ---------------------------------------------------------------------
  // Derived data
  // ---------------------------------------------------------------------

  /// Distinct sorted category names from the loaded events.
  List<String> get _categories {
    final set = <String>{'All'};
    for (final e in _events) {
      if (e.category.trim().isNotEmpty) set.add(e.category);
    }
    final list = set.toList();
    list.sort((a, b) {
      if (a == 'All') return -1;
      if (b == 'All') return 1;
      return a.compareTo(b);
    });
    return list;
  }

  /// Events filtered by the search query and selected category.
  List<EventModel> get _filteredEvents {
    final query = _searchController.text.trim().toLowerCase();

    return _events.where((e) {
      // Category filter
      if (_selectedCategory != 'All' && e.category != _selectedCategory) {
        return false;
      }
      // Search filter
      if (query.isEmpty) return true;
      return e.title.toLowerCase().contains(query) ||
          (e.description?.toLowerCase().contains(query) ?? false) ||
          e.category.toLowerCase().contains(query);
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title:
            Text(_selectedDestination == 0 ? 'Discover Events' : 'Attendance'),
        actions: _selectedDestination == 0
            ? [
                IconButton(
                  icon: const Icon(Icons.refresh),
                  tooltip: 'Refresh',
                  onPressed: _loadEvents,
                ),
                PopupMenuButton<String>(
                  icon: const Icon(Icons.account_circle_outlined),
                  tooltip: 'Account',
                  onSelected: (value) async {
                    switch (value) {
                      case 'profile':
                        _openProfile();
                        break;
                      case 'applications':
                        _openMyApplications();
                        break;
                      case 'shifts':
                        _openMyShifts();
                        break;
                      case 'logout':
                        await _handleLogout();
                        break;
                    }
                  },
                  itemBuilder: (context) => const [
                    PopupMenuItem(
                      value: 'profile',
                      child: ListTile(
                        leading: Icon(Icons.person_outline),
                        title: Text('My Profile'),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                    PopupMenuItem(
                      value: 'applications',
                      child: ListTile(
                        leading: Icon(Icons.description_outlined),
                        title: Text('My Applications'),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                    PopupMenuItem(
                      value: 'shifts',
                      child: ListTile(
                        leading: Icon(Icons.schedule_outlined),
                        title: Text('My Shifts'),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                    PopupMenuDivider(),
                    PopupMenuItem(
                      value: 'logout',
                      child: ListTile(
                        leading: Icon(Icons.logout, color: Colors.red),
                        title: Text('Logout',
                            style: TextStyle(color: Colors.red)),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                  ],
                ),
              ]
            : null,
      ),
      body: _selectedDestination == 0 ? _buildBody() : const AttendanceScreen(),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedDestination,
        onDestinationSelected: (index) {
          setState(() => _selectedDestination = index);
        },
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.explore_outlined),
            selectedIcon: Icon(Icons.explore),
            label: 'Discover',
          ),
          NavigationDestination(
            icon: Icon(Icons.fact_check_outlined),
            selectedIcon: Icon(Icons.fact_check),
            label: 'Attendance',
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return _buildErrorState();
    }

    if (_events.isEmpty) {
      return _buildEmptyState();
    }

    return Column(
      children: [
        // ---- Search bar ----
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
          child: TextField(
            controller: _searchController,
            decoration: InputDecoration(
              hintText: 'Search events…',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: _searchController.text.isEmpty
                  ? null
                  : IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: () => _searchController.clear(),
                    ),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
              ),
              isDense: true,
              contentPadding: const EdgeInsets.symmetric(vertical: 12),
            ),
          ),
        ),

        // ---- Category filter chips ----
        if (_categories.length > 1)
          SizedBox(
            height: 42,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12),
              itemCount: _categories.length,
              separatorBuilder: (_, __) => const SizedBox(width: 8),
              itemBuilder: (context, i) {
                final cat = _categories[i];
                final selected = cat == _selectedCategory;
                return ChoiceChip(
                  label: Text(cat),
                  selected: selected,
                  onSelected: (_) =>
                      setState(() => _selectedCategory = cat),
                );
              },
            ),
          ),

        // ---- Events list ----
        Expanded(
          child: _filteredEvents.isEmpty
              ? _buildNoResultsState()
              : RefreshIndicator(
                  onRefresh: _loadEvents,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(12),
                    itemCount: _filteredEvents.length,
                    itemBuilder: (context, index) {
                      final event = _filteredEvents[index];
                      return EventCard(
                        event: event,
                        onTap: () => _openDetail(event),
                      );
                    },
                  ),
                ),
        ),
      ],
    );
  }

  Widget _buildErrorState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 64, color: Colors.redAccent),
            const SizedBox(height: 16),
            const Text(
              'Could not load events',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              _errorMessage ?? 'Unknown error',
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.black54),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: _loadEvents,
              icon: const Icon(Icons.refresh),
              label: const Text('Retry'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.event_busy, size: 64, color: Colors.grey),
            const SizedBox(height: 16),
            const Text(
              'No events yet',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Check back later — new events will appear here.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.black54),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: _loadEvents,
              icon: const Icon(Icons.refresh),
              label: const Text('Refresh'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildNoResultsState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.search_off, size: 64, color: Colors.grey),
            const SizedBox(height: 16),
            const Text(
              'No matching events',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Try clearing the search or choosing a different category.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.black54),
            ),
            const SizedBox(height: 24),
            OutlinedButton.icon(
              onPressed: () {
                _searchController.clear();
                setState(() => _selectedCategory = 'All');
              },
              icon: const Icon(Icons.clear_all),
              label: const Text('Clear filters'),
            ),
          ],
        ),
      ),
    );
  }

  void _openDetail(EventModel event) {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => EventDetailScreen(eventId: event.id),
      ),
    );
  }
}