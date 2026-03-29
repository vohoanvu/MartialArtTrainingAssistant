import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import LandingPage from '@/pages/LandingPage';

const renderLandingPage = () =>
  render(
    <MemoryRouter>
      <LandingPage />
    </MemoryRouter>
  );

describe('LandingPage', () => {
  it('renders the hero section with the main heading', () => {
    renderLandingPage();
    expect(
      screen.getByText(/Revolutionize Your BJJ Dojo with AI Assistance/i)
    ).toBeInTheDocument();
  });

  it('renders the Get Beta Access CTA button', () => {
    renderLandingPage();
    expect(
      screen.getByRole('button', { name: /Join the AI Revolution/i })
    ).toBeInTheDocument();
  });

  it('renders the features section heading', () => {
    renderLandingPage();
    expect(
      screen.getByText(/Empower Your Coaching/i)
    ).toBeInTheDocument();
  });

  it('renders the beta launch section heading', () => {
    renderLandingPage();
    expect(
      screen.getByText(/Ready to Transform Your BJJ Dojo/i)
    ).toBeInTheDocument();
  });
});
