# EventCrew Volunteer Mobile Application

**Student 2 Mobile Application & Cross-Platform Integration**  
SE3090 Section 8 (Mobile Application), Section 10 (Cross-Platform Integration), and Section 11 (Third-Party Integration).

---

## 📱 Architecture & Features Overview

### 1. Section 8 — Mobile Application Compliance
- **Encrypted Session Persistence**: Authenticated JWT tokens and user identities are stored in device hardware keystores using `flutter_secure_storage` (iOS Keychain / Android Keystore).
- **Mandatory Device Hardware Feature**: Profile screen integrates `image_picker` allowing volunteers to capture a live profile avatar photo via the hardware camera or select from the device gallery.
- **State Management**: Reactive multi-provider state architecture (`provider`) decoupling UI components from network models.

### 2. Section 10 — Cross-Platform Integration
- **Dynamic API Base URL Resolution**:
  - **Android Emulator**: Automatically routes to `http://10.0.2.2:5100/api` (host loopback alias).
  - **iOS Simulator & macOS Desktop**: Automatically routes to `http://localhost:5100/api`.
- **JWT Authorization Interceptor**: Automatically injects `Authorization: Bearer <token>` on all protected endpoints.
- **Cross-Platform Synchronization**: Applications submitted on mobile reflect immediately in the organizer's React web dashboard (`/frontend-web`), and status updates made by organizers are reflected live in the mobile app.

### 3. Section 11 — Third-Party Integration
- **Transactional Confirmation Emails**:
  - Welcome confirmation email triggered on volunteer registration (`POST /api/auth/register`).
  - Application receipt email triggered on event application (`POST /api/applications`).
- **Resilience & Secret Protection**:
  - Secrets loaded securely via `EMAIL_API_KEY`.
  - Enforces a 5-second timeout and try-catch fallback. In local development or offline mode, dispatches structured console logs without failing user operations.

---

## 🚀 Running the Mobile Application

### Prerequisites
1. Ensure the backend ASP.NET Core API is running:
   ```bash
   dotnet run --project src/EventCrew.Api/EventCrew.Api.csproj
   ```
   API runs on `http://localhost:5100`.

2. Navigate into `frontend-mobile`:
   ```bash
   cd frontend-mobile
   ```

3. Install Flutter packages:
   ```bash
   flutter pub get
   ```

4. Run on your desired target:
   - **Android Emulator**:
     ```bash
     flutter run -d emulator
     ```
   - **iOS Simulator**:
     ```bash
     flutter run -d iPhone
     ```
   - **macOS Desktop**:
     ```bash
     flutter run -d macos
     ```
   - **Web (Chrome)**:
     ```bash
     flutter run -d chrome
     ```

---

## ⚡ Instant Viva Demonstration

On the Login screen, use the **Instant Viva Demo Login** pills:
- ⚡ **Login as Samadhi**: pre-fills `samadhi@eventcrew.com` / `password123`
- ⚡ **Login as Shareeka**: pre-fills `shareeka@eventcrew.com` / `password123`

### Key Demo Flows to Showcase
1. **Explore Events**: View published events like *TechFest 2026*, tap "View Details" to see dates, venue, and required roles.
2. **Apply to Volunteer**: Tap "Apply to Volunteer", pick a preferred role, add notes, and submit. An official receipt popup appears and transactional email is dispatched.
3. **Application Status Tracking**: Switch to "My Applications" tab to see real-time color-coded badges (🟡 Submitted, 🔵 Under Review, 🟢 Approved, 🔴 Rejected).
4. **Hardware Camera Profile**: Switch to "My Profile" tab, tap the camera icon on the avatar to trigger device camera/gallery, select multiple skills with interactive FilterChips, and save.
