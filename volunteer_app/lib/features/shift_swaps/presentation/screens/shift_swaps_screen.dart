import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../core/api/api_client.dart';
import '../../../shifts/data/repositories/shift_repository.dart';
import '../../data/models/shift_swap_model.dart';
import '../../data/repositories/shift_swap_repository.dart';
import 'create_swap_screen.dart';

/// Screen displaying incoming and outgoing shift swap requests for the volunteer.
class ShiftSwapsScreen extends StatefulWidget {
  const ShiftSwapsScreen({super.key, this.apiClient});

  final ApiClient? apiClient;

  @override
  State<ShiftSwapsScreen> createState() => _ShiftSwapsScreenState();
}

class _ShiftSwapsScreenState extends State<ShiftSwapsScreen> {
  late final ShiftSwapRepository _swapRepository = ShiftSwapRepository(
    apiClient: widget.apiClient,
  );
  late final ShiftRepository _shiftRepository = ShiftRepository(
    apiClient: widget.apiClient,
  );

  bool _isLoading = true;
  String? _errorMessage;
  List<ShiftSwapModel> _swaps = [];
  String? _volunteerProfileId;
  bool _hasConfirmedAssignment = false;
  String? _actionInProgressId;

  @override
  void initState() {
    super.initState();
    _loadSwaps();
  }

  Future<void> _loadSwaps() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final assignments = await _shiftRepository.getMyAssignments();
      final swaps = await _swapRepository.getShiftSwaps();
      if (!mounted) return;
      setState(() {
        _volunteerProfileId = assignments.volunteerId;
        _hasConfirmedAssignment = assignments.assignments.any(
          (assignment) => assignment.status == 'Confirmed',
        );
        _swaps = swaps;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString();
        _isLoading = false;
      });
    }
  }

  Future<void> _handleAccept(String swapId) async {
    setState(() => _actionInProgressId = swapId);
    try {
      await _swapRepository.acceptShiftSwap(swapId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Swap request accepted! Awaiting organizer approval.',
            ),
            backgroundColor: Colors.green,
          ),
        );
      }
      await _loadSwaps();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _actionInProgressId = null);
    }
  }

  Future<void> _handleReject(String swapId) async {
    if (!await _confirmAction(
      title: 'Decline swap request?',
      message: 'This request will be marked as rejected.',
      confirmLabel: 'Decline',
    )) {
      return;
    }
    setState(() => _actionInProgressId = swapId);
    try {
      await _swapRepository.rejectShiftSwap(swapId);
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Swap request declined.')));
      }
      await _loadSwaps();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _actionInProgressId = null);
    }
  }

  Future<void> _handleCancel(String swapId) async {
    if (!await _confirmAction(
      title: 'Cancel swap request?',
      message: 'The other volunteer will no longer be able to accept it.',
      confirmLabel: 'Cancel request',
    )) {
      return;
    }
    setState(() => _actionInProgressId = swapId);
    try {
      await _swapRepository.cancelShiftSwap(swapId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Swap request cancelled.')),
        );
      }
      await _loadSwaps();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _actionInProgressId = null);
    }
  }

  Future<bool> _confirmAction({
    required String title,
    required String message,
    required String confirmLabel,
  }) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Keep request'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(confirmLabel),
          ),
        ],
      ),
    );
    return confirmed == true;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Shift Swaps'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadSwaps),
        ],
      ),
      body: Column(
        children: [
          if (_volunteerProfileId != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
              child: InputDecorator(
                decoration: const InputDecoration(
                  labelText: 'Your volunteer profile ID',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
                child: SelectableText(_volunteerProfileId!),
              ),
            ),
          if (!_isLoading && _errorMessage == null && !_hasConfirmedAssignment)
            const Padding(
              padding: EdgeInsets.fromLTRB(16, 12, 16, 0),
              child: Text(
                'A confirmed shift assignment is required to request a swap.',
                style: TextStyle(color: Colors.black54),
              ),
            ),
          Expanded(child: _buildBody()),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _hasConfirmedAssignment
            ? () async {
                final created = await Navigator.of(context).push<bool>(
                  MaterialPageRoute<bool>(
                    builder: (_) =>
                        CreateSwapScreen(apiClient: widget.apiClient),
                  ),
                );
                if (created == true && mounted) {
                  await _loadSwaps();
                }
              }
            : null,
        icon: const Icon(Icons.add),
        label: const Text('Request swap'),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.error_outline,
                size: 64,
                color: Colors.redAccent,
              ),
              const SizedBox(height: 16),
              const Text(
                'Could not load shift swaps',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(_errorMessage!, textAlign: TextAlign.center),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: _loadSwaps,
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (_swaps.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.swap_horizontal_circle_outlined,
                size: 64,
                color: Colors.grey,
              ),
              const SizedBox(height: 16),
              const Text(
                'No shift swap requests',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              const Text(
                'Shift swap requests you create or receive will appear here.',
                textAlign: TextAlign.center,
                style: TextStyle(color: Colors.black54),
              ),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: _loadSwaps,
                icon: const Icon(Icons.refresh),
                label: const Text('Refresh'),
              ),
            ],
          ),
        ),
      );
    }

    final volunteerProfileId = _volunteerProfileId;

    return RefreshIndicator(
      onRefresh: _loadSwaps,
      child: ListView.builder(
        padding: const EdgeInsets.all(12),
        itemCount: _swaps.length,
        itemBuilder: (context, index) {
          final swap = _swaps[index];
          final isIncoming = swap.targetVolunteerId == volunteerProfileId;
          final isRequester = swap.requesterVolunteerId == volunteerProfileId;
          final isBusy = _actionInProgressId == swap.id;

          return Card(
            margin: const EdgeInsets.only(bottom: 12),
            elevation: 2,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: isIncoming
                              ? Colors.blue.shade50
                              : Colors.purple.shade50,
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Text(
                          isIncoming ? 'INCOMING REQUEST' : 'OUTGOING REQUEST',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: isIncoming
                                ? Colors.blue.shade700
                                : Colors.purple.shade700,
                          ),
                        ),
                      ),
                      _buildStatusChip(swap.status),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'Source Shift: ${swap.sourceShiftTitle}',
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(
                    'Event: ${swap.sourceEventTitle}',
                    style: const TextStyle(
                      fontSize: 13,
                      color: Colors.deepPurple,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Requester: ${swap.requesterName} (${swap.requesterEmail})',
                  ),
                  Text('Target Volunteer: ${swap.targetVolunteerName}'),
                  Text(
                    'Target Shift: ${swap.targetShiftTitle} · ${swap.targetEventTitle}',
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Requested ${DateFormat('MMM d, yyyy · h:mm a').format(swap.createdAt)}',
                    style: const TextStyle(fontSize: 12, color: Colors.black54),
                  ),
                  if (swap.reason != null && swap.reason!.isNotEmpty) ...[
                    const SizedBox(height: 6),
                    Text(
                      'Reason: "${swap.reason}"',
                      style: const TextStyle(
                        fontStyle: FontStyle.italic,
                        color: Colors.black87,
                      ),
                    ),
                  ],
                  if (swap.status == 'Pending_Organizer') ...[
                    const SizedBox(height: 10),
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: Colors.amber.shade50,
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: const Row(
                        children: [
                          Icon(
                            Icons.hourglass_top,
                            size: 16,
                            color: Colors.amber,
                          ),
                          SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'Waiting for organizer approval',
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: Colors.amber,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      if (isIncoming && swap.status == 'Pending_Target') ...[
                        ElevatedButton.icon(
                          onPressed: isBusy
                              ? null
                              : () => _handleAccept(swap.id),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.green,
                            foregroundColor: Colors.white,
                          ),
                          icon: const Icon(Icons.check, size: 18),
                          label: const Text('Accept'),
                        ),
                        const SizedBox(width: 8),
                        OutlinedButton.icon(
                          onPressed: isBusy
                              ? null
                              : () => _handleReject(swap.id),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: Colors.red,
                          ),
                          icon: const Icon(Icons.close, size: 18),
                          label: const Text('Decline'),
                        ),
                      ],
                      if (isRequester &&
                          (swap.status == 'Pending_Target' ||
                              swap.status == 'Pending_Organizer')) ...[
                        OutlinedButton.icon(
                          onPressed: isBusy
                              ? null
                              : () => _handleCancel(swap.id),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: Colors.red,
                          ),
                          icon: const Icon(Icons.cancel_outlined, size: 18),
                          label: const Text('Cancel Request'),
                        ),
                      ],
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildStatusChip(String status) {
    Color bg;
    Color fg;
    String label = status.replaceAll('_', ' ');

    switch (status) {
      case 'Approved':
        bg = Colors.green.shade50;
        fg = Colors.green.shade700;
        break;
      case 'Rejected':
        bg = Colors.red.shade50;
        fg = Colors.red.shade700;
        break;
      case 'Cancelled':
        bg = Colors.grey.shade100;
        fg = Colors.grey.shade700;
        break;
      case 'Pending_Organizer':
        bg = Colors.amber.shade50;
        fg = Colors.amber.shade800;
        break;
      case 'Pending_Target':
        bg = Colors.blue.shade50;
        fg = Colors.blue.shade700;
        break;
      default:
        bg = Colors.grey.shade100;
        fg = Colors.grey.shade700;
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        label,
        style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: fg),
      ),
    );
  }
}
