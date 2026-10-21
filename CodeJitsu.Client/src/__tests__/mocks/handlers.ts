import { http, HttpResponse } from 'msw';
import type { AiFeatureSettings } from '@/components/AiConsole/types';
import { mockOverview, mockQuotaConfig, mockQuotaStatus, mockTestResult } from './aiConsoleData';

// ---------------------------------------------------------------------------
// Shared mock data
// ---------------------------------------------------------------------------

const mockTrainingSessions = [
  {
    id: 1,
    trainingDate: '2026-03-26T09:00:00',
    description: 'Morning BJJ Session',
    capacity: 20,
    duration: 1.5,
    status: 'Active',
    targetLevel: 'Beginner',
    instructorId: 1,
  },
  {
    id: 2,
    trainingDate: '2026-03-26T18:00:00',
    description: 'Evening Judo Session',
    capacity: 15,
    duration: 2.0,
    status: 'Active',
    targetLevel: 'Intermediate',
    instructorId: 1,
  },
];

const mockSessionDetail = {
  id: 1,
  title: 'Morning BJJ Session',
  date: '2026-03-26T09:00:00',
  location: 'Main Dojo',
  instructorId: 1,
  isOpen: true,
  martialArt: 'BJJ',
  attendees: [],
};

const mockFighterInfo = {
  id: 1,
  userId: 'user-abc-123',
  firstName: 'John',
  lastName: 'Doe',
  beltLevel: 'Blue',
  martialArt: 'BJJ',
  weight: 75,
  height: 180,
};

const mockFighter = {
  id: 1,
  userId: 'user-abc-123',
  firstName: 'John',
  lastName: 'Doe',
  beltLevel: 'Blue',
  martialArt: 'BJJ',
  weight: 75,
  height: 180,
};

const mockSharedVideos = [
  {
    id: 1,
    videoUrl: 'https://www.youtube.com/watch?v=abc123',
    title: 'BJJ Guard Pass Tutorial',
    description: 'Learn the basics of guard passing',
    sharedBy: { userId: 'user-abc-123', username: 'johndoe' },
    uploadedAt: '2026-03-01T10:00:00',
  },
];

const mockUploadedVideos = [
  {
    id: 1,
    fileName: 'sparring_session_01.mp4',
    description: 'Saturday sparring session',
    studentIdentifier: 'John Doe',
    martialArt: 'BJJ',
    signedUrl: 'https://storage.example.com/videos/sparring_session_01.mp4',
    uploadedAt: '2026-03-20T10:00:00',
  },
];

export const mockTechniqueSearchResults = {
  source: 'vertex-grounded',
  model_id: 'gemini-4-argon',
  youtube_videos: [
    {
      video_id: 'abc123',
      title: 'Guard Passing Tutorial',
      description: 'Learn guard passing techniques',
      embed_link: 'https://www.youtube.com/embed/abc123',
      publication_date: '2024-01-01T00:00:00+00:00',
      thumbnail_url: 'https://i.ytimg.com/vi/abc123/mqdefault.jpg',
      channel_title: 'Grappling Academy',
      why_relevant: 'Covers the knee-slice pass step by step.',
    },
  ],
  web_resources: [
    {
      title: 'Knee slice breakdown',
      url: 'https://example.com/knee-slice',
      description: 'Detailed article',
      is_free: true,
    },
    {
      title: 'Passing course',
      url: 'https://example.com/course',
      description: null,
      is_free: false,
    },
  ],
  web_search_queries: ['knee slice pass bjj'],
  search_entry_point_html: '<div class="container"><a class="chip" href="https://www.google.com/search?q=knee+slice">knee slice pass bjj</a></div>',
};

const mockLoginResponse = {
  tokenType: 'Bearer',
  accessToken: 'mock-access-token',
  expiresIn: 3600,
  refreshToken: 'mock-refresh-token',
};

const mockCurriculum = {
  id: 1,
  sessionId: 1,
  content: 'Warm-up: 10 minutes jogging\nTechnique: Guard passing fundamentals\nSparring: 30 minutes',
  generatedAt: '2026-03-26T08:00:00',
};

// ---------------------------------------------------------------------------
// MSW handlers mapped to the actual API routes used in src/services/api.ts
// ---------------------------------------------------------------------------

export const handlers = [
  // Auth: Login
  http.post('/api/fighter/login', () => {
    return HttpResponse.json(mockLoginResponse);
  }),

  // Auth: Register
  http.post('/api/auth/v1/register', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // Auth: Refresh token
  http.post('/api/auth/v1/refresh', () => {
    return HttpResponse.json(mockLoginResponse);
  }),

  // Training Sessions: GET all
  http.get('/api/trainingsession', () => {
    return HttpResponse.json(mockTrainingSessions);
  }),

  // Training Sessions: POST create
  http.post('/api/trainingsession', () => {
    return HttpResponse.json({
      id: 3,
      title: 'New Session',
      date: '2026-03-27T09:00:00',
      location: 'Main Dojo',
      instructorId: 1,
      isOpen: true,
      martialArt: 'BJJ',
    });
  }),

  // Training Sessions: GET by ID
  http.get('/api/trainingsession/:sessionId', ({ params }) => {
    const { sessionId } = params;
    if (sessionId === '999') {
      return new HttpResponse(null, { status: 404 });
    }
    return HttpResponse.json(mockSessionDetail);
  }),

  // Training Sessions: PUT update by ID
  http.put('/api/trainingsession/:sessionId', () => {
    return HttpResponse.json(mockSessionDetail);
  }),

  // Training Sessions: DELETE by ID
  http.delete('/api/trainingsession/:sessionId', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // Training Sessions: PATCH close
  http.patch('/api/trainingsession/:sessionId/close', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // Training Sessions: DELETE remove attendee
  http.delete('/api/trainingsession/:sessionId/attendance/remove/:fighterId', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // Training Sessions: POST take walk-in attendance
  http.post('/api/trainingsession/:sessionId/attendance', () => {
    return HttpResponse.json({ success: true, attendeeId: 42 });
  }),

  // Fighter: GET authenticated fighter info
  http.get('/api/fighter/info', () => {
    return HttpResponse.json(mockFighterInfo);
  }),

  // Fighter: GET by ID
  http.get('/api/fighter/:fighterId', ({ params }) => {
    const { fighterId } = params;
    if (fighterId === '999') {
      return new HttpResponse(null, { status: 404 });
    }
    return HttpResponse.json(mockFighter);
  }),

  // Fighter: POST join waitlist
  http.post('/api/fighter/join-waitlist', () => {
    return new HttpResponse(null, { status: 200 });
  }),

  // VideoAnalysis: GET all shared videos
  http.get('/vid/api/video/getall', () => {
    return HttpResponse.json(mockSharedVideos);
  }),

  // VideoAnalysis: POST upload YouTube video metadata
  http.post('/vid/api/video/metadata', () => {
    return HttpResponse.json(mockSharedVideos[0]);
  }),

  // VideoAnalysis: GET all uploaded (GCS) videos
  http.get('/vid/api/video/getall-uploaded', () => {
    return HttpResponse.json(mockUploadedVideos);
  }),

  // VideoAnalysis: GET free demo analysis quota (must precede the generic /video/:videoId route)
  http.get('/vid/api/video/analysis-quota', () => HttpResponse.json(mockQuotaStatus())),

  // VideoAnalysis: POST request a direct-to-GCS signed upload URL (quota pre-check lives here)
  http.post('/vid/api/video/upload-url', () =>
    HttpResponse.json({
      videoId: 10,
      uploadUrl: 'https://storage.example.com/upload/10',
      gcsPath: 'videos/10.mp4',
    })
  ),

  // VideoAnalysis: POST start analysis (reserves a quota use)
  http.post('/vid/api/video/analyze-v2/:videoId', () => new HttpResponse(null, { status: 202 })),
  http.post('/vid/api/video/analyze/:videoId', () => new HttpResponse(null, { status: 202 })),

  // VideoAnalysis: GET uploaded video by ID
  http.get('/vid/api/video/:videoId', ({ params }) => {
    const { videoId } = params;
    if (videoId === '999') {
      return new HttpResponse(null, { status: 404 });
    }
    return HttpResponse.json(mockUploadedVideos[0]);
  }),

  // VideoAnalysis: GET video feedback/analysis
  http.get('/vid/api/video/:videoId/feedback', () => {
    return HttpResponse.json({
      id: 1,
      videoId: '1',
      feedback: 'Good guard passing technique. Work on hip movement.',
      analyzedAt: '2026-03-20T12:00:00',
    });
  }),

  // VideoAnalysis: PATCH save video analysis result
  http.patch('/vid/api/video/:videoId/analysis', () => {
    return HttpResponse.json({
      id: 1,
      videoId: '1',
      feedback: 'Updated analysis result.',
      analyzedAt: '2026-03-26T10:00:00',
    });
  }),

  // VideoAnalysis: DELETE uploaded video
  http.delete('/vid/api/video/delete-uploaded/:videoId', () => {
    return HttpResponse.json({ success: true, message: 'Video deleted successfully' });
  }),

  // VideoAnalysis: POST upload sparring video file
  http.post('/vid/api/video/upload-sparring', () => {
    return HttpResponse.json({
      message: 'Video uploaded successfully',
      videoId: 10,
      signedUrl: 'https://storage.example.com/videos/new_upload.mp4',
      isDuplicate: false,
    });
  }),

  // VideoAnalysis: GET generate class curriculum
  http.get('/vid/api/video/session/:sessionId/generate', () => {
    return HttpResponse.json(mockCurriculum);
  }),

  // VideoAnalysis: GET existing class curriculum
  http.get('/vid/api/video/:sessionId/curriculum', () => {
    return HttpResponse.json(mockCurriculum);
  }),

  // VideoAnalysis: POST match maker for session
  http.post('/vid/api/video/matchmaker/:sessionId', () => {
    return HttpResponse.json({
      isSuccessfullyParsed: true,
      rawFighterPairsJson: '',
      errorMessage: null,
      suggestedPairings: {
        pairs: [
          { student1: { studentId: 1, studentName: 'John Doe' }, student2: { studentId: 2, studentName: 'Jane Smith' } },
        ],
        unpairedStudent: null,
        pairingRationale: 'Matched by belt level and weight class.',
      },
    });
  }),

  // MatchMaker: POST generate fighter pairs
  http.post('/pair/api/matchmaker', () => {
    return HttpResponse.json({
      pairs: [
        { fighter1Id: 1, fighter2Id: 2 },
      ],
    });
  }),

  // YouTube: POST technique search (Vertex-grounded payload; returned as a JSON string body)
  http.post('/vid/api/youtube/search', () => {
    return HttpResponse.text(JSON.stringify(mockTechniqueSearchResults));
  }),

  // AI Console (admin-only)
  http.get('/vid/api/ai-console/access', () => HttpResponse.json({ isAdmin: true })),

  http.get('/vid/api/ai-console', () => HttpResponse.json(mockOverview())),

  http.put('/vid/api/ai-console/features/:key', async ({ params, request }) => {
    const body = (await request.json()) as AiFeatureSettings;
    const feature = mockOverview().features.find((f) => f.key === params.key);
    if (!feature) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json({
      ...feature,
      isOverridden: true,
      updatedAt: '2026-10-21T08:30:00Z',
      updatedBy: 'admin@example.com',
      effective: body,
    });
  }),

  http.delete('/vid/api/ai-console/features/:key', ({ params }) => {
    const feature = mockOverview().features.find((f) => f.key === params.key);
    if (!feature) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json({
      ...feature,
      isOverridden: false,
      updatedAt: null,
      updatedBy: null,
      effective: feature.defaults,
    });
  }),

  http.post('/vid/api/ai-console/features/:key/test', () => HttpResponse.json(mockTestResult)),

  http.get('/vid/api/ai-console/quota', () => HttpResponse.json(mockQuotaConfig())),

  http.put('/vid/api/ai-console/quota', async ({ request }) => {
    const body = await request.json();
    return HttpResponse.json(
      mockQuotaConfig({
        isOverridden: true,
        updatedAt: '2026-10-21T08:30:00Z',
        updatedBy: 'admin@example.com',
        effective: body as never,
      })
    );
  }),

  http.delete('/vid/api/ai-console/quota', () => HttpResponse.json(mockQuotaConfig())),
];

