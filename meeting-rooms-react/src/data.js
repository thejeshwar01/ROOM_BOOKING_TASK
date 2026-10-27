// Display-only lookups. All real data (rooms, users, reservations) comes from the API.

// ///EQUIPMENT ICONS///
// Keys match the equipment names returned by the API
export const equipmentList = {
    projector: { icon: "bi-projector", label: "Projector" },
    tvScreen: { icon: "bi-tv", label: "TV Screen" },
    videoConferencing: { icon: "bi-person-video3", label: "Video Conferencing" },
    whiteboard: { icon: "bi-easel", label: "Whiteboard" }
};


// ///ROOM PHOTOS///
// Keyed by room code
export const roomImages = {
    "RM-001": "/images/bluenova.jpg",
    "RM-002": "/images/cyborg.jpg",
    "RM-003": "/images/daxi.jpg",
    "RM-004": "/images/aurora.jpg",
    "RM-005": "/images/phantom.jpg",
    "RM-006": "/images/tiger.jpg"
};


// ///RESERVATION STATUSES///
export const statusClass = {
    "Booked": "status-booked",
    "Completed": "status-completed",
    "Cancelled": "status-cancelled"
};
