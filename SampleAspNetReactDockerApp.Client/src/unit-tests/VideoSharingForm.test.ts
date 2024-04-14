import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import SharingVideo from '@/pages/SharingVideo';
import React from 'react';
import '@testing-library/jest-dom';
import { act } from 'react-dom/test-utils';
import FetchMock, {enableFetchMocks} from 'jest-fetch-mock';
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(globalThis as any).fetch = FetchMock;
enableFetchMocks();


describe('VideoSharingForm', () => {
    beforeEach(() => {
        const mockSharedVideoResponse  = {
            id: 'videoId1',
            title: 'Test Video 1',
            embedLink: 'https://www.example.com/embed/video1',
            sharedBy: { userId: 'userid1', username: 'user1' },
            description: 'Description for Test Video 1',
        };
        jest.mock('@/services/api', () => ({
            uploadYoutubeVideo: jest.fn().mockReturnValue(mockSharedVideoResponse),
            useAuthStore: jest.fn().mockReturnValue(mockAuthStore),
        }));
        const mockAuthStore = {
            accessToken: 'mockAccessToken',
            refreshToken: 'mockRefreshToken',
        };
        FetchMock.mockResponseOnce(JSON.stringify(mockSharedVideoResponse));
    });

    it('renders without crashing', async () => {
        await act(async () => {
            render(React.createElement(SharingVideo));
        });
    });

    it('renders form fields correctly', async () => {
        await act(async () => {
            render(React.createElement(SharingVideo));
        });
        expect(screen.getByLabelText('Youtube URL')).toBeInTheDocument();
    });

    it('handles form submission', async () => {
        await act(async () => {
            render(React.createElement(SharingVideo));
        });
    
        const input = screen.getByLabelText('Youtube URL');
        const button = screen.getByText('Share');
        fireEvent.change(input, { target: { value: 'https://youtube.com/watch?v=dQw4w9WgXcQ' } });
        fireEvent.click(button);

        await waitFor(() => {
            // Asserting that the input field is cleared after form submission
            expect(input).toHaveValue('');
        });
    });
});