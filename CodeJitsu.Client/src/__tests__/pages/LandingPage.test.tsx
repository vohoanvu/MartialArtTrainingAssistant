import { describe, it, expect, beforeAll } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import LandingPage from '@/pages/LandingPage';
import { initTestI18n } from '../helpers/i18nTest';

const renderLandingPage = () =>
  render(
    <MemoryRouter>
      <LandingPage />
    </MemoryRouter>
  );

describe('LandingPage', () => {
  beforeAll(async () => {
    await initTestI18n();
  });

  it('renders the hero with the demo framing', () => {
    renderLandingPage();
    expect(screen.getByRole('heading', { level: 1, name: /AI that reads a BJJ sparring tape/i })).toBeInTheDocument();
    expect(screen.getByText(/Demo project · Internal R&D tool/i)).toBeInTheDocument();
  });

  it('uses the self-hosted demo video as the hero media', () => {
    renderLandingPage();
    const video = screen.getByLabelText(/CodeJitsu demo video/i);
    expect(video.tagName).toBe('VIDEO');
    expect(video).toHaveAttribute('src', '/videos/codejitsu-demo.mp4');
    expect(video).toHaveAttribute('poster', '/videos/codejitsu-demo-poster.jpg');
  });

  it('states clearly that this is not a commercial product', () => {
    renderLandingPage();
    expect(screen.getByText(/A demo, not a commercial product/i)).toBeInTheDocument();
    expect(screen.getByText(/Not for sale/i)).toBeInTheDocument();
    expect(screen.getByText(/Used internally for R&D/i)).toBeInTheDocument();
    expect(screen.getByText(/It is not a commercial product\./i)).toBeInTheDocument();
  });

  it('no longer markets beta access, pricing or testimonials', () => {
    renderLandingPage();
    expect(screen.queryByText(/Beta/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Pricing/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Testimonials/i)).not.toBeInTheDocument();
    expect(document.querySelector('a[href="/pricing"]')).toBeNull();
  });

  it('credits TKNQ and links to its website', () => {
    renderLandingPage();
    expect(screen.getByText(/internal R&D project by TKNQ \(pronounced “Technique”\)/i)).toBeInTheDocument();
    const link = screen.getByRole('link', { name: /Visit TKNQ/i });
    expect(link).toHaveAttribute('href', 'https://tknq.io/');
    expect(link).toHaveAttribute('rel', 'noopener noreferrer');
  });

  it('links the demo CTA to sign-in', () => {
    renderLandingPage();
    expect(screen.getByRole('button', { name: /Try the demo/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Open the demo/i })).toHaveAttribute('href', '/home');
  });
});
