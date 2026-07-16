let googleTokenClient = null;
let googleAccessToken = null;

function initGoogleAuth() {
    googleTokenClient = google.accounts.oauth2.initTokenClient({
        client_id: "907059686920-6fpi0nt2ar7v9pfbkmtta427um4t9smb.apps.googleusercontent.com",
        scope: "https://www.googleapis.com/auth/calendar.events",
        callback: (response) => {
            googleAccessToken = response.access_token;
        }
    });
}

function getGoogleAccessToken() {
    return new Promise((resolve, reject) => {
        if (googleAccessToken) {
            resolve(googleAccessToken);
            return;
        }
        googleTokenClient.callback = (response) => {
            if (response.error) {
                reject(response.error);
                return;
            }
            googleAccessToken = response.access_token;
            resolve(googleAccessToken);
        };
        googleTokenClient.requestAccessToken();
    });
}

async function createGoogleCalendarEvent(interview, accessToken, personLabel) {
    const start = new Date(interview.scheduledTime);
    const end = new Date(start.getTime() + 60 * 60000);

    const eventPayload = {
        summary: `Interview: ${interview.jobTitle} with ${personLabel}`,
        description: `Interview for ${interview.jobTitle}\nWith: ${personLabel}\nLocation/Link: ${interview.locationOrLink || 'TBA'}\nNotes: ${interview.notes || 'None'}`,
        location: interview.locationOrLink || 'TBA',
        start: { dateTime: start.toISOString() },
        end: { dateTime: end.toISOString() }
    };

    const response = await fetch(
        "https://www.googleapis.com/calendar/v3/calendars/primary/events",
        {
            method: "POST",
            headers: {
                "Authorization": `Bearer ${accessToken}`,
                "Content-Type": "application/json"
            },
            body: JSON.stringify(eventPayload)
        }
    );

    if (!response.ok) {
        const errorData = await response.json();
        console.error("Google Calendar API error:", errorData);
        throw new Error("Failed to create calendar event");
    }
}

initGoogleAuth();