import LandingPageForm from "@/components/LandingPageForm";
import { ReactElement } from "react";
import { PageWrapper } from "@/components/zen/PageWrapper";

export function Home(): ReactElement {
    return (
        <PageWrapper>
            <div className="flex flex-col items-center">
                <section className="max-w-lg mx-auto mt-6 flex flex-col items-center text-center">
                    <h1 className="font-serif text-3xl font-bold text-ink-400 mb-6">
                        Your AI-driven Brazilian JiuJitsu Training Assistant
                    </h1>
                </section>
                <section className="w-full max-w-md">
                    <LandingPageForm />
                </section>
            </div>
        </PageWrapper>
    );
}
