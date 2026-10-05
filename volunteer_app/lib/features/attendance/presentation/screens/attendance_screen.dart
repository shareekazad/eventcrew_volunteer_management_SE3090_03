import 'package:flutter/material.dart';

import '../../../../core/api/api_client.dart';
import '../../../events/data/models/event_model.dart';
import '../../../events/data/repositories/event_repository.dart';
import '../../data/models/attendance_record.dart';
import '../../data/repositories/attendance_repository.dart';
import 'qr_scanner_screen.dart';

class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({
    super.key,
    AttendanceRepository? attendanceRepository,
    EventRepository? eventRepository,
  })  : _attendanceRepository = attendanceRepository,
        _eventRepository = eventRepository;

  final AttendanceRepository? _attendanceRepository;
  final EventRepository? _eventRepository;

  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends State<AttendanceScreen> {
  late final AttendanceRepository _attendanceRepository =
      widget._attendanceRepository ?? AttendanceRepository();
  late final EventRepository _eventRepository =
      widget._eventRepository ?? EventRepository();
  final _volunteerIdController = TextEditingController();
  final _shiftIdController = TextEditingController();

  List<EventModel> _events = [];
  List<AttendanceRecord> _history = [];
  EventModel? _selectedEvent;
  AttendanceRecord? _attendance;
  String? _eventsError;
  String? _attendanceError;
  String? _historyError;
  String? _scannedToken;
  bool _eventsLoading = true;
  bool _stateLoading = false;
  bool _stateLoaded = false;
  bool _historyLoading = false;
  bool _submitting = false;
  int _stateRequestId = 0;
  String? _failedAttendanceAction;

  String get _volunteerId => _volunteerIdController.text.trim();
  String get _shiftId => _shiftIdController.text.trim();

  bool get _hasValidIdentifiers =>
      _isGuid(_volunteerId) &&
      _isGuid(_shiftId) &&
      _selectedEvent != null;

  @override
  void initState() {
    super.initState();
    _loadEvents();
    _volunteerIdController.addListener(_onIdentifiersChanged);
    _shiftIdController.addListener(_onIdentifiersChanged);
  }

  @override
  void dispose() {
    _volunteerIdController
      ..removeListener(_onIdentifiersChanged)
      ..dispose();
    _shiftIdController
      ..removeListener(_onIdentifiersChanged)
      ..dispose();
    super.dispose();
  }

  void _onIdentifiersChanged() {
    _stateRequestId++;
    if (_attendance != null || _attendanceError != null) {
      setState(() {
        _attendance = null;
        _attendanceError = null;
        _stateLoaded = false;
        _stateLoading = false;
      });
    } else {
      setState(() {
        _stateLoaded = false;
        _stateLoading = false;
      });
    }
  }

  Future<void> _loadEvents() async {
    setState(() {
      _eventsLoading = true;
      _eventsError = null;
    });
    try {
      final events = await _eventRepository.getAllEvents();
      if (!mounted) return;
      setState(() {
        _events = events;
        _eventsLoading = false;
        if (_selectedEvent != null) {
          final selectedEventId = _selectedEvent!.id;
          _selectedEvent = _findEvent(events, selectedEventId);
        }
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _eventsError = error.toString();
        _eventsLoading = false;
      });
    }
  }

  Future<void> _scanQr() async {
    final token = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const QrScannerScreen()),
    );
    if (!mounted || token == null) return;
    setState(() {
      _scannedToken = token.trim();
      _attendanceError = null;
    });
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('QR code scanned. Ready to check in.')),
    );
  }

  Future<void> _loadAttendanceState() async {
    if (!_hasValidIdentifiers) {
      setState(() {
        _attendanceError =
            'Enter a valid volunteer profile ID and shift ID, then select an event.';
      });
      return;
    }

    final requestId = ++_stateRequestId;
    setState(() {
      _stateLoading = true;
      _attendanceError = null;
    });
    try {
      final records = await _attendanceRepository.getEventAttendance(
        eventId: _selectedEvent!.id,
        shiftId: _shiftId,
      );
      if (!mounted || requestId != _stateRequestId) return;
      final matchingRecord = records.where(
        (record) => record.volunteerId.toLowerCase() == _volunteerId.toLowerCase(),
      );
      setState(() {
        _attendance = matchingRecord.isEmpty ? null : matchingRecord.first;
        _stateLoading = false;
        _stateLoaded = true;
        _failedAttendanceAction = null;
      });
    } catch (error) {
      if (!mounted || requestId != _stateRequestId) return;
      setState(() {
        _attendanceError = _messageFor(error);
        _stateLoading = false;
        _failedAttendanceAction = 'refresh';
      });
    }
  }

  Future<void> _checkIn() async {
    if (!_hasValidIdentifiers || _scannedToken == null) return;
    setState(() {
      _submitting = true;
      _attendanceError = null;
    });
    try {
      final record = await _attendanceRepository.checkIn(
        volunteerId: _volunteerId,
        eventId: _selectedEvent!.id,
        shiftId: _shiftId,
        token: _scannedToken!,
      );
      if (!mounted) return;
      setState(() {
        _attendance = record;
        _scannedToken = null;
        _submitting = false;
        _stateLoaded = true;
        _failedAttendanceAction = null;
      });
      _showMessage('Check-in successful.');
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _attendanceError = _messageFor(error);
        _submitting = false;
        _failedAttendanceAction = 'checkIn';
      });
    }
  }

  Future<void> _checkOut() async {
    final attendanceId = _attendance?.id;
    if (attendanceId == null || !_isGuid(attendanceId)) {
      setState(() {
        _attendanceError = 'A checked-in attendance record could not be found.';
      });
      return;
    }
    setState(() {
      _submitting = true;
      _attendanceError = null;
    });
    try {
      final record = await _attendanceRepository.checkOut(
        attendanceId: attendanceId,
        volunteerId: _volunteerId,
      );
      if (!mounted) return;
      setState(() {
        _attendance = record;
        _submitting = false;
        _stateLoaded = true;
        _failedAttendanceAction = null;
      });
      _showMessage('Check-out successful.');
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _attendanceError = _messageFor(error);
        _submitting = false;
        _failedAttendanceAction = 'checkOut';
      });
    }
  }

  Future<void> _loadHistory() async {
    if (!_isGuid(_volunteerId)) {
      setState(() => _historyError = 'Enter a valid volunteer profile ID first.');
      return;
    }
    setState(() {
      _historyLoading = true;
      _historyError = null;
    });
    try {
      final records = await _attendanceRepository.getVolunteerHistory(_volunteerId);
      if (!mounted) return;
      setState(() {
        _history = records;
        _historyLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _historyError = _messageFor(error);
        _historyLoading = false;
      });
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  String _messageFor(Object error) {
    if (error is! ApiException) return error.toString();
    final message = error.message.toLowerCase();
    if (error.statusCode == 404) return 'The selected event or shift could not be found.';
    if (message.contains('expired')) return 'This QR code has expired. Ask the organizer for a new code.';
    if (message.contains('qr token') && message.contains('invalid')) {
      return 'This QR code is invalid. Scan the event attendance code and try again.';
    }
    if (message.contains('state that allows check-in')) {
      return 'This attendance is already checked in or cannot be checked in again.';
    }
    if (message.contains('state that allows check-out')) {
      return 'You are not currently checked in, or this attendance was already checked out.';
    }
    if (message.contains('not assigned')) {
      return 'Your volunteer profile is not assigned to this shift.';
    }
    if (message.contains('confirmed assignment')) {
      return 'Your shift assignment is not confirmed.';
    }
    return error.message;
  }

  bool _isGuid(String value) => RegExp(
        r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
      ).hasMatch(value);

  EventModel? _findEvent(List<EventModel> events, String id) {
    for (final event in events) {
      if (event.id == id) return event;
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Check in to your shift',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: 8),
                Text(
                  'Select the event and enter your assigned shift and volunteer profile IDs. Scan the organizer’s QR code to check in.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 16),
                if (_eventsLoading)
                  const LinearProgressIndicator()
                else if (_eventsError != null)
                  _InlineError(
                    message: _eventsError!,
                    onRetry: _loadEvents,
                  )
                else
                  DropdownButtonFormField<EventModel>(
                    key: ValueKey(_selectedEvent?.id),
                    initialValue: _selectedEvent,
                    decoration: const InputDecoration(
                      labelText: 'Event',
                      border: OutlineInputBorder(),
                    ),
                    items: _events
                        .map(
                          (event) => DropdownMenuItem(
                            value: event,
                            child: Text(event.title, overflow: TextOverflow.ellipsis),
                          ),
                        )
                        .toList(),
                    onChanged: _submitting
                        ? null
                        : (event) {
                                setState(() {
                                  _stateRequestId++;
                                  _selectedEvent = event;
                                  _attendance = null;
                                  _attendanceError = null;
                                  _stateLoaded = false;
                                  _scannedToken = null;
                                });
                              },
                  ),
                const SizedBox(height: 12),
                TextField(
                  controller: _volunteerIdController,
                  enabled: !_submitting,
                  decoration: const InputDecoration(
                    labelText: 'Volunteer profile ID',
                    helperText: 'Use the profile ID supplied by your organizer.',
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.text,
                  textCapitalization: TextCapitalization.none,
                  autocorrect: false,
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _shiftIdController,
                  enabled: !_submitting,
                  decoration: const InputDecoration(
                    labelText: 'Assigned shift ID',
                    helperText: 'Use the shift ID supplied by your organizer.',
                    border: OutlineInputBorder(),
                  ),
                  autocorrect: false,
                ),
                const SizedBox(height: 16),
                Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children: [
                    OutlinedButton.icon(
                      onPressed: _submitting ? null : _scanQr,
                      icon: const Icon(Icons.qr_code_scanner),
                      label: Text(_scannedToken == null ? 'Scan QR code' : 'Scan again'),
                    ),
                    FilledButton.icon(
                      onPressed: !_hasValidIdentifiers || _scannedToken == null || _submitting
                          ? null
                          : _checkIn,
                      icon: _submitting
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.login),
                      label: Text(
                        _failedAttendanceAction == 'checkIn'
                            ? 'Retry check-in'
                            : 'Check in',
                      ),
                    ),
                    TextButton.icon(
                      onPressed: !_hasValidIdentifiers || _stateLoading || _submitting
                          ? null
                          : _loadAttendanceState,
                      icon: const Icon(Icons.refresh),
                      label: Text(_stateLoaded ? 'Refresh state' : 'Load current state'),
                    ),
                  ],
                ),
                if (_scannedToken != null) ...[
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Icon(Icons.verified_outlined, color: colors.primary, size: 18),
                      const SizedBox(width: 6),
                      const Expanded(child: Text('QR scanned. Token is hidden for your security.')),
                      IconButton(
                        tooltip: 'Clear scanned QR',
                        onPressed: () => setState(() => _scannedToken = null),
                        icon: const Icon(Icons.close),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        if (_attendanceError != null)
          _InlineError(
            message: _attendanceError!,
            onRetry: !_hasValidIdentifiers
                ? null
                : _failedAttendanceAction == 'checkIn'
                    ? _checkIn
                    : _failedAttendanceAction == 'checkOut'
                        ? _checkOut
                        : _loadAttendanceState,
          ),
        if (_stateLoading) ...[
          const SizedBox(height: 8),
          const Center(child: CircularProgressIndicator()),
        ],
        if (!_stateLoading && _stateLoaded && _hasValidIdentifiers) ...[
          const SizedBox(height: 8),
          _AttendanceStateCard(
            record: _attendance,
            eventTitle: _selectedEvent!.title,
            onCheckOut: _attendance?.status == 'CheckedIn' && !_submitting
                ? _checkOut
                : null,
            submitting: _submitting,
          ),
        ],
        const SizedBox(height: 12),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Participation history', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 4),
                const Text('Review attendance records associated with your volunteer profile.'),
                const SizedBox(height: 12),
                OutlinedButton.icon(
                  onPressed: _historyLoading ? null : _loadHistory,
                  icon: _historyLoading
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.history),
                  label: Text(_historyLoading ? 'Loading history…' : 'Load my history'),
                ),
                if (_historyError != null) ...[
                  const SizedBox(height: 8),
                  _InlineError(message: _historyError!, onRetry: _loadHistory),
                ],
                if (!_historyLoading && _historyError == null && _history.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  ..._history.map((record) => _HistoryTile(record: record)),
                ],
                if (!_historyLoading && _historyError == null && _history.isEmpty)
                  const Padding(
                    padding: EdgeInsets.only(top: 8),
                    child: Text('Load your history to view past participation.'),
                  ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _AttendanceStateCard extends StatelessWidget {
  const _AttendanceStateCard({
    required this.record,
    required this.eventTitle,
    required this.onCheckOut,
    required this.submitting,
  });

  final AttendanceRecord? record;
  final String eventTitle;
  final VoidCallback? onCheckOut;
  final bool submitting;

  @override
  Widget build(BuildContext context) {
    final status = record?.status ?? 'Pending';
    final checkedIn = status == 'CheckedIn';
    final checkedOut = status == 'CheckedOut';
    final colors = Theme.of(context).colorScheme;
    final statusColor = checkedIn
        ? colors.primary
        : checkedOut
            ? colors.tertiary
            : colors.outline;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Current attendance', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            Row(
              children: [
                Icon(
                  checkedIn
                      ? Icons.radio_button_checked
                      : checkedOut
                          ? Icons.task_alt
                          : Icons.pending_outlined,
                  color: statusColor,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    checkedIn
                        ? 'Currently checked in'
                        : checkedOut
                            ? 'Shift checked out'
                            : 'Not checked in',
                    style: TextStyle(color: statusColor, fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(eventTitle),
            if (record?.checkInTime != null)
              _AttendanceTimeRow(label: 'Checked in', value: record!.checkInTime!),
            if (record?.checkOutTime != null)
              _AttendanceTimeRow(label: 'Checked out', value: record!.checkOutTime!),
            if (checkedOut)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text('Verified participation: ${record!.verifiedHours.toStringAsFixed(2)} hours'),
              ),
            if (checkedIn) ...[
              const SizedBox(height: 12),
              FilledButton.tonalIcon(
                onPressed: submitting ? null : onCheckOut,
                icon: submitting
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.logout),
                label: const Text('Check out'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _AttendanceTimeRow extends StatelessWidget {
  const _AttendanceTimeRow({required this.label, required this.value});

  final String label;
  final DateTime value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 8),
      child: Row(
        children: [
          SizedBox(width: 100, child: Text(label)),
          Expanded(child: Text(value.toLocal().toString())),
        ],
      ),
    );
  }
}

class _HistoryTile extends StatelessWidget {
  const _HistoryTile({required this.record});

  final AttendanceRecord record;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: const CircleAvatar(child: Icon(Icons.event_available_outlined)),
      title: Text(record.eventTitle.isEmpty ? 'Event attendance' : record.eventTitle),
      subtitle: Text(
        '${record.shiftTitle} • ${record.status} • '
        '${record.verifiedHours.toStringAsFixed(2)} hours',
      ),
      isThreeLine: true,
      trailing: const Icon(Icons.chevron_right),
    );
  }
}

class _InlineError extends StatelessWidget {
  const _InlineError({required this.message, required this.onRetry});

  final String message;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: colors.errorContainer,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Icon(Icons.error_outline, color: colors.onErrorContainer),
          const SizedBox(width: 8),
          Expanded(child: Text(message)),
          if (onRetry != null)
            TextButton(onPressed: onRetry, child: const Text('Retry')),
        ],
      ),
    );
  }
}
