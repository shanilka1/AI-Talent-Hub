function addToGoogleCalendar(interview, personLabel) {
    const start = new Date(interview.scheduledTime);
    const end = new Date(start.getTime() + 60 * 60000); // 1 hour duration

    const formatDate = (date) => {
        return date.toISOString().replace(/-|:|\.\d+/g, '');
    };

    const summary = `Interview: ${interview.jobTitle} with ${personLabel}`;
    const description = `Interview for ${interview.jobTitle}\nWith: ${personLabel}\nLocation/Link: ${interview.locationOrLink || 'TBA'}\nNotes: ${interview.notes || 'None'}`;
    const location = interview.locationOrLink || 'TBA';

    const url = new URL('https://calendar.google.com/calendar/render');
    url.searchParams.append('action', 'TEMPLATE');
    url.searchParams.append('text', summary);
    url.searchParams.append('dates', `${formatDate(start)}/${formatDate(end)}`);
    url.searchParams.append('details', description);
    url.searchParams.append('location', location);

    window.open(url.toString(), '_blank');
}